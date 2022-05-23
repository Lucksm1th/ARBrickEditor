/*
 * Inspired by: https://github.com/mxgmn/WaveFunctionCollapse
The MIT License(MIT)
Copyright(c) mxgmn 2016.
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
The software is provided "as is", without warranty of any kind, express or implied, including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement. In no event shall the authors or copyright holders be liable for any claim, damages or other liability, whether in an action of contract, tort or otherwise, arising from, out of or in connection with the software or the use or other dealings in the software.
*/

using System;
using System.Linq;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Xml;

static class HelpersWFC
{
    public static int Random(this float[] a, float r)
    {
        float sum = a.Sum();

        if (sum == 0f)
        {
            for (int j = 0; j < a.Count(); j++)
                a[j] = 1.0f;
            sum = a.Sum();
        }

        for (int j = 0; j < a.Count(); j++)
            a[j] /= sum;

        int i = 0;
        float partialSum = 0f;

        while (i < a.Count())
        {
            partialSum += a[i];
            if (r < partialSum)
                return i;
            i++;
        }

        return 0;
    }

    public static T Get<T>(this XElement xelem, string attribute, T defaultT = default(T))
    {
        XAttribute a = xelem.Attribute(attribute);
        return a == null ? defaultT : (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(a.Value);
    }

    public static float GetFloat(this XElement xelem, string attribute, float defaultT = default(float))
    {
        XAttribute a = xelem.Attribute(attribute);
        return a == null ? defaultT : (float)TypeDescriptor.GetConverter(typeof(float)).ConvertFromInvariantString(a.Value);
    }

    public static T Get<T>(this XmlNode node, string attribute, T defaultT = default(T))
    {
        string s = ((XmlElement)node).GetAttribute(attribute);
        var converter = TypeDescriptor.GetConverter(typeof(T));
        return s == "" ? defaultT : (T)converter.ConvertFromString(s);
    }

    public static float GetFloat(this XmlNode node, string attribute, float defaultT = default(float))
    {
        string s = ((XmlElement)node).GetAttribute(attribute);
        return s == "" ? defaultT : float.Parse(s);
    }

    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }

    public static IEnumerable<XElement> Elements(this XElement xelement, params string[] names) => xelement.Elements().Where(e => names.Any(n => n == e.Name));
}
