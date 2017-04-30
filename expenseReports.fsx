open System
open System.IO

type ExpenseReportLine = {
    Day: DateTime
    Description: string
    Amount: decimal
    VAT: VAT list
    ReceiptLocation: string
}
and VAT = {
    Rate: decimal
    Amount: decimal
    AmountExcludingTaxes: decimal
}

let parseDay (parts:array<string>) = DateTime.ParseExact(parts.[0], "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)

let parseDescription (parts:array<string>) = parts.[1].Replace("+", " ")

let decimal' (s:string) = decimal (s.Replace("e", "")) 

let parseAmount (parts:array<string>) = decimal' parts.[2]

let vat vatRate (vatParts: string array) amountIncludingTax nbVat =
    match vatRate, vatParts.Length, nbVat with
    | 0m, 1, _ ->
        { Rate = vatRate; Amount = 0m; AmountExcludingTaxes = amountIncludingTax }
    | _, 2, 1 ->
        let vatAmount = decimal' vatParts.[1]
        { 
            Rate = vatRate; 
            Amount = vatAmount;
            AmountExcludingTaxes = amountIncludingTax - vatAmount
        }
    | _, 2, _ when vatParts.[1].Contains("+") ->
        let vatAmounts = vatParts.[1].Split('+')
        let vatAmount = decimal' vatAmounts.[1]
        { 
            Rate = vatRate; 
            Amount = vatAmount;
            AmountExcludingTaxes = decimal' vatAmounts.[0]
        }
    | _, 2, _ -> 
        let vatAmount = decimal' vatParts.[1]
        let amountExcludingVat = vatAmount / vatRate * 100m
        let assumeAmountIncludingVatHasNoCentsUnit = Decimal.Round(amountExcludingVat + vatAmount, 1, MidpointRounding.AwayFromZero)
        let diff = Math.Abs(amountExcludingVat + vatAmount - assumeAmountIncludingVatHasNoCentsUnit)
        { 
            Rate = vatRate; 
            Amount = vatAmount;
            AmountExcludingTaxes = amountExcludingVat + diff
        }
    | _, _, _ -> failwithf "Unknown case with VAT rate %M and amounts '%s'" vatRate (vatParts |> String.concat "=") 

let rec parseVAT' (parts:array<string>) remaining totalAmount nbVat =
    if remaining > 0 then
        let vatParts = parts.[parts.Length - remaining].Split('=')
        let vatRate =
            match vatParts.[0] with
            | "VAT0" | "TVA0" -> 0m
            | "VAT5.5" | "TVA5.5" -> 5.5m
            | "VAT10" | "TVA10" -> 10m
            | "VAT20" | "TVA20" -> 20m
            | unknown -> failwithf "Unknown VAT %s" unknown
        List.append [ vat vatRate vatParts totalAmount nbVat ] (parseVAT' parts (remaining - 1) totalAmount nbVat)
    else
        []

let parseVAT (parts:array<string>) totalAmount =
    let nbVAT = parts.Length - 3
    parseVAT' parts nbVAT totalAmount nbVAT

let parseReceiptFileName file =
    let fileInfo = new FileInfo(file)
    printfn "Generating line for %s" fileInfo.Name
    let parts = fileInfo.Name.Replace(fileInfo.Extension, "").Split('-')
    let totalAmount = parseAmount parts
    let vat = parseVAT parts totalAmount
    if vat |> Seq.fold (fun x y -> x + y.Amount + y.AmountExcludingTaxes) 0m <> totalAmount then
        failwithf "Please give excluding taxes amount, it cannot be inferred..."
    {
        Day = parseDay parts
        Description = parseDescription parts;
        Amount = totalAmount;
        VAT = vat
        ReceiptLocation = file
    }

let getReceipts directory =
    let receipts = 
        Directory.EnumerateFiles(directory, "*.jpg")
        |> Seq.append <| Directory.EnumerateFiles(directory, "*.pdf")
    receipts
    |> Seq.map parseReceiptFileName

let formatDecimal (decimal:decimal) =
    decimal.ToString("G")

let writeCsv directory =
    let lines = 
        getReceipts directory
        |> Seq.sortBy (fun x -> x.Day)
        |> Seq.collect (fun x -> 
            x.VAT
            |> Seq.map (fun vat -> 
                                sprintf "%s;%s;%s;%s;%s;%s" 
                                    (x.Day.ToString("dd/MM/yyyy")) 
                                    x.Description
                                    (vat.Rate / 100m |> formatDecimal)
                                    (vat.AmountExcludingTaxes + vat.Amount |> formatDecimal)
                                    (vat.AmountExcludingTaxes |> formatDecimal)
                                    (vat.Amount |> formatDecimal)))

    File.WriteAllLines(Path.Combine(directory, "expenseReport.csv"), lines, System.Text.Encoding.UTF8)

let directory = "C:/Users/cleme/OneDrive - DevCrafting/Administratif/Comptabilité/Achats/2017_04/Note de frais"

writeCsv directory