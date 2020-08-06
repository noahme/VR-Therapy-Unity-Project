using System.Collections.Generic;
using System.Xml.Serialization;

[XmlRoot("CueCollection")]
public class CueContainer
{
    [XmlArray("Cues")]
    [XmlArrayItem("Cue")]
    public List<Cue> cues = new List<Cue>();
}
