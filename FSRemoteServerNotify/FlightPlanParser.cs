using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace FSRemoteServerNotify
{

    public struct struct_Waypoint
    {
        public double latitude;
        public double longitude;
    }

    class FlightPlanParser
    {

        private string plnfile;
        public List<struct_Waypoint> waypoints;

        public FlightPlanParser(string filename)
        {
            plnfile = filename;
            ParseFLT();
        }

        private void ParseFLT()
        {
            if (plnfile == "")
                return;

            waypoints = new List<struct_Waypoint>();

            XDocument plnReader = XDocument.Load(plnfile);
            var waypointStrings = from x in plnReader.Descendants("ATCWaypoint")
                            select new
                            {
                                wayP = x.Descendants("WorldPosition").First().Value
                            };

            foreach (var wp in waypointStrings)
            {
                waypoints.Add(stringToWaypoint(wp.wayP));
            }
        }

        private struct_Waypoint stringToWaypoint(string s)
        {
            struct_Waypoint sw;

            double lt, ln;
            lt = 0.0;
            ln = 0.0;

            Regex re = new Regex(@"([N|S])([0-9]*[\.[0-9]+]*)°\s([0-9]*[\.[0-9]+]*)'\s([0-9]*[\.[0-9]+]*)"",([W|E])([0-9]*[\.[0-9]+]*)°\s([0-9]*[\.[0-9]+]*)'\s([0-9]*[\.[0-9]+]*)""");
            Match m = re.Match(s);

            if (m.Success)
            {
                lt = Convert.ToDouble(m.Groups[2].Value) + (Convert.ToDouble(m.Groups[3].Value) / 60.0) + (Convert.ToDouble(m.Groups[4].Value) / 3600.0);
                if (m.Groups[1].Value.Contains("S"))
                {
                    lt *= -1.0;
                }
                ln = Convert.ToDouble(m.Groups[6].Value) + (Convert.ToDouble(m.Groups[7].Value) / 60.0) + (Convert.ToDouble(m.Groups[8].Value) / 3600.0);
                if (m.Groups[5].Value.Contains("W"))
                {
                    ln *= -1.0;
                }
            }

            sw.latitude = lt;
            sw.longitude = ln;

            return (sw);
        }

    }
}
