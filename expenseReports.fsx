open System
open System.IO

type ExpenseReportLine = {
    Day: DateTime
    Description: string
    Amount: decimal
    TVA: TVA list
    ReceiptLocation: string
}
and TVA = {
    Percent: decimal
    Amount: decimal
}

let parseDay (parts:array<string>) = DateTime.ParseExact(parts.[0], "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture)

let parseDescription (parts:array<string>) = parts.[1]

let decimal' (s:string) = decimal (s.Replace("e", "")) 

let parseAmount (parts:array<string>) = decimal' parts.[2]

let rec parseTVA' (parts:array<string>) remaining =
    if remaining > 0 then
        let tvaParts = parts.[parts.Length - remaining].Split('=')
        let tva =
            match tvaParts.[0] with
            | "TVA0" -> { Percent = 0m; Amount = 0m }
            | "TVA5.5" -> { Percent = 5.5m; Amount = decimal' tvaParts.[1]}
            | "TVA10" -> { Percent = 10m; Amount = decimal' tvaParts.[1]}
            | "TVA20" -> { Percent = 20m; Amount = decimal' tvaParts.[1]}
            | unknown -> failwithf "Unknown TVA %s" unknown 
        List.append [ tva ] (parseTVA' parts (remaining - 1))
    else
        []

let parseTVA (parts:array<string>) =
    let nbTVA = parts.Length - 3
    parseTVA' parts nbTVA

let parseReceiptFileName file =
    let fileInfo = new FileInfo(file)
    let parts = fileInfo.Name.Replace(fileInfo.Extension, "").Split('-')
    {
        Day = parseDay parts
        Description = parseDescription parts;
        Amount = parseAmount parts;
        TVA = parseTVA parts
        ReceiptLocation = file
    }

let getReceipts directory =
    let receipts = Directory.EnumerateFiles(directory, "*.jpg")
    receipts
    |> Seq.map parseReceiptFileName

let formatDecimal (decimal:decimal) =
    decimal.ToString("G")

let writeCsv directory =
    let lines = 
        getReceipts directory
        |> Seq.sortBy (fun x -> x.Day)
        |> Seq.collect (fun x -> 
            x.TVA
            |> Seq.map (fun tva -> sprintf "%s;%s;%s;%s;%s;%s" 
                                    (x.Day.ToString("dd/MM/yyyy")) 
                                    x.Description
                                    (tva.Percent / 100m |> formatDecimal)
                                    (x.Amount |> formatDecimal)
                                    (x.Amount - tva.Amount |> formatDecimal)
                                    (tva.Amount |> formatDecimal)))

    File.WriteAllLines(Path.Combine(directory, "expenseReport.csv"), lines, System.Text.Encoding.UTF8)

let directory = "C:/Users/cleme/OneDrive Entreprise/Administratif/Comptabilité/Achats/2016/2016_12/Note de frais"

writeCsv directory