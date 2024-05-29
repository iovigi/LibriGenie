using System.Xml.Serialization;

namespace LibriGenie.Workers.Services.ContentGenerate.Models;

[XmlRoot("GeneratedContent")]
public class GeneratedContent
{
    [XmlElement(ElementName = "title")]
    public string Title { get; set; }
    [XmlElement(ElementName = "content")]
    public string Content { get; set; }
}
