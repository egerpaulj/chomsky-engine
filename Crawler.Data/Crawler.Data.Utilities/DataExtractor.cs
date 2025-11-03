using System.Text;
using Crawler.Core.Parser.File;
using LanguageExt;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace Crawler.Data.Uitilities;

public class DataExtractor : IDataExtractor
{
    public TryOptionAsync<string> ExtractFromDocx(Option<byte[]> docBytes)
    {
        throw new NotImplementedException();
    }

    public TryOptionAsync<string> ExtractFromPdf(Option<byte[]> pdfBytes)
    {
        return pdfBytes
            .ToTryOptionAsync()
            .Bind<byte[], string>(bytes =>
                async () =>
                {
                    var builder = new StringBuilder();
                    using (PdfDocument document = PdfDocument.Open(bytes))
                    {
                        var pageNumber = 1;
                        foreach (var page in document.GetPages())
                        {
                            builder.AppendLine($"# Page: {pageNumber}");
                            builder.AppendLine("--------------------");
                            builder.AppendLine();
                            builder.AppendLine(
                                ContentOrderTextExtractor.GetText(page, addDoubleNewline: true)
                            );

                            pageNumber++;
                        }
                    }

                    return await Task.FromResult(builder.ToString());
                }
            );
    }
}
