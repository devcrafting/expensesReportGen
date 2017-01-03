This script generate an expense report CSV file from a list of expense's receipts files. These files must follow a naming convention to generate the expense report file.

My usage of this script
=======================

To track my professional expenses, I take a picture of each expense receipt with Office Lens from my phone (available Android, IOS and Windows Mobile). It is really great to keep a digital version of your receipts, you just take a picture without too much care of angle/zoom, and it crops and transform to have a picture as if you scanned it.

Then, since I need to fill in an expense reports regularly, I thought I could encode expense information in picture name, to be able to generate the expense report. That's what the script does with files named as follow : `yyyyMMdd-[expense description]-[amount]-[list of VAT separated by "-"]`, where

- each VAT is formatted : `VAT[%]=[VAT amount]`, can use TVA (in French) instead of VAT, % is restricted to given VAT rates (French ones configured in the script)
- amount (and VAT amount) uses "." for decimal, can add symbol (like e) for the currency (but don't do any conversion for now)

So to generate the expense report, just give the folder containing files to process, it will generate a CSV file with the following columns: `Date (dd/MM/yyyy);Description;VAT rate;amount VAT included;amount without VAT;VAT amount`.
