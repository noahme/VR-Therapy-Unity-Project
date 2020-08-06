using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
using System;

public class Cue : IComparable<Cue>
{
    [XmlAttribute("prefabName")]
    public string prefabName;

    [XmlElement("weight")]
    public float weight;

    [XmlElement("type")]
    public string type;

    [XmlElement("category")]
    public string category;

    public int CompareTo(Cue other)
    {
        if (other == null)
        {
            return 1;
        }

        if (weight - other.weight > 0)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }
}