//-----------------------------------------------------------------------------
// Camera Singleton that for now, doesn't do much.
//
// __Defense Sample for Game Programming Algorithms and Techniques
// Copyright (C) Sanjay Madhav. All rights reserved.
//
// Released under the Microsoft Permissive License.
// See LICENSE.txt for full details.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Xml;
using System.IO;
using System.Xml.Linq;

namespace itp380
{
    public class Segment
    {
        public int x { get; set; }
        public int width { get; set; }
        public int y { get; set; }
        public int height { get; set; }
        public int z { get; set; }
        public int length { get; set; }
        public int texture { get; set; }
    }

    public class XMLParser : itp380.Patterns.Singleton<XMLParser>
    {  
        public void parseXML(int[, ,] levelArray) {
            List<Segment> segments;
            Segment currentSegment;

            System.IO.Stream stream = TitleContainer.OpenStream("Content\\level.xml");

            XDocument doc = XDocument.Load(stream);
            segments = (from segment in doc.Descendants("segment")
                select new Segment()
                {
                        x = Convert.ToInt32(segment.Element("x").Value),
                     
                        width = Convert.ToInt32(segment.Element("width").Value),
                        
                        y = Convert.ToInt32(segment.Element("y").Value),
                        
                        height = Convert.ToInt32(segment.Element("height").Value),
                       
                        z = Convert.ToInt32(segment.Element("z").Value),
                        
                        length = Convert.ToInt32(segment.Element("length").Value),
                       
                        texture = Convert.ToInt32(segment.Element("texture").Value),

                }).ToList();

                for(int segmentNumber = 0; segmentNumber < segments.Count(); segmentNumber++) {
                    currentSegment = segments.ElementAt(segmentNumber);
                    for(int x = currentSegment.x; x < currentSegment.x + currentSegment.width; x++) {
                        for (int y = currentSegment.y; y < currentSegment.y + currentSegment.height; y++)
                        {
                            for (int z = currentSegment.z; z < currentSegment.z + currentSegment.length; z++)
                            {
                                levelArray[x, y, z] = currentSegment.texture;
                            }
                        }
                    }
                }
            }

    }
}

