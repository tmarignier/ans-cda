using System.Xml.Linq;
using CdaCrImg.Serialization;

namespace CdaCrImg.Tests;

public class CrImgWriterLevel1Tests
{
    private static readonly XNamespace V3 = CdaNamespaces.Hl7;

    private static XElement Root() => CrImgWriter.Write(SampleReports.Level1()).Root!;

    [Fact]
    public void Document_IsValidAgainstCdaXsd()
    {
        Assert.Empty(CdaXsdValidator.Validate(CrImgWriter.Write(SampleReports.Level1())));
    }

    [Fact]
    public void Document_HasOnlyTheThreeNonStructuredTemplateIds()
    {
        var roots = Root().Elements(V3 + "templateId").Select(t => (string?)t.Attribute("root"));

        Assert.Equal(new[] { TemplateIds.Document.Hl7France, TemplateIds.Document.CiSis, TemplateIds.Document.NonStructuredBody }, roots);
    }

    [Fact]
    public void DocumentCode_Is18748_4_WithOneTranslationPerActe()
    {
        var code = Root().Element(V3 + "code")!;

        Assert.Equal(Codes.DocumentType, (string?)code.Attribute("code"));
        Assert.Equal(new[] { "24727-0", "36235-0" }, code.Elements(V3 + "translation").Select(t => (string?)t.Attribute("code")));
    }

    [Fact]
    public void Body_EmbedsPdfInBase64()
    {
        var text = Root().Element(V3 + "component")!.Element(V3 + "nonXMLBody")!.Element(V3 + "text")!;

        Assert.Equal("application/pdf", (string?)text.Attribute("mediaType"));
        Assert.Equal("B64", (string?)text.Attribute("representation"));
        Assert.Equal(SampleReports.AnsPdf, Convert.FromBase64String(text.Value));
    }

    [Fact]
    public void InFulfillmentOf_HasOrderIdAndDicomAccessionNumber()
    {
        var orders = Root().Elements(V3 + "inFulfillmentOf").Select(i => i.Element(V3 + "order")!).ToList();

        Assert.Equal(2, orders.Count);
        Assert.All(orders, o => Assert.NotNull(o.Element(CdaNamespaces.Ps320 + "accessionNumber")));
        Assert.Equal("UNK", (string?)orders[1].Element(V3 + "id")!.Attribute("nullFlavor"));
    }

    [Fact]
    public void ServiceEvent_HasStudyUidRootOnly_AndQualifiedTranslations()
    {
        var serviceEvent = Root().Elements(V3 + "documentationOf").First().Element(V3 + "serviceEvent")!;
        var id = serviceEvent.Element(V3 + "id")!;
        var translations = serviceEvent.Element(V3 + "code")!.Elements(V3 + "translation").ToList();

        Assert.Equal("1.2.250.1.925.994044.27.123.1876360", (string?)id.Attribute("root"));
        Assert.Null(id.Attribute("extension"));
        Assert.Contains(translations, t => (string?)t.Attribute("codeSystem") == CodeSystems.Ccam);
        Assert.Contains(translations, t => (string?)t.Attribute("code") == "CT"
            && (string?)t.Element(V3 + "qualifier")!.Element(V3 + "name")!.Attribute("code") == Codes.Entries.DcmModality);
        Assert.Contains(translations, t => (string?)t.Attribute("code") == "774007"
            && (string?)t.Element(V3 + "qualifier")!.Element(V3 + "name")!.Attribute("code") == Codes.Entries.LoincAnatomicLocation);
        Assert.NotNull(serviceEvent.Element(V3 + "performer"));
    }

    [Fact]
    public void Screening_AddsZ13_9DocumentationOf()
    {
        var codes = Root().Elements(V3 + "documentationOf")
            .Select(d => (string?)d.Element(V3 + "serviceEvent")!.Element(V3 + "code")!.Attribute("code"));

        Assert.Equal(new[] { "24727-0", "36235-0", "Z13.9" }, codes);
    }

    [Fact]
    public void Output_IsDeterministic()
    {
        Assert.Equal(CrImgWriter.WriteToString(SampleReports.Level1()), CrImgWriter.WriteToString(SampleReports.Level1()));
    }

    [Fact]
    public void WriteToString_ProducesUtf8XmlDeclaration()
    {
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", CrImgWriter.WriteToString(SampleReports.Level1()));
    }
}
