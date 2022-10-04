using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MagBlazor.OAModels
{
    public class SpecialObjects
    {
        private IEnumerable<XElement> _funds = null;
        public IEnumerable<XElement> funds 
        { 
            get { return _funds; } 
        }
        //public IComparer<string> comparedates { get { throw new Exception("Err: comparedates does not implemented"); } }
        private OAData.IFactographDataService db;
        //private SpecialObjects so;
        public SpecialObjects (OAData.IFactographDataService db)
        {
            this.db = db;
            //so = new SpecialObjects(db);
            _funds = new XElement[0];
        }
        private string funds_id = null;
        public string GetField(XElement rec, string prop)
        {
            //string lang = null;
            string res = null;
            foreach (XElement f in rec.Elements("field"))
            {
                string p = f.Attribute("prop").Value;
                if (p != prop) continue;
                XAttribute xlang = f.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                res = f.Value;
                if (xlang?.Value == "ru") { break; }
            }
            return res;
        }
        private string[] months = new[] { "янв", "фев", "мар", "апр", "май", "июн", "июл", "авг", "сен", "окт", "ноя", "дек" };
        public string DatePrinted(string date)
        {
            if (date == null) return null;
            string[] split = date.Split('-');
            string str = split[0];
            if (split.Length > 1)
            {
                int month;
                if (Int32.TryParse(split[1], out month) && month > 0 && month <= 12)
                {
                    str += months[month - 1];
                    if (split.Length > 2) str += split[2].Substring(0, 2);
                }
            }
            return str;
        }
        public string GetDates(XElement item)
        {
            var fd_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/from-date");
            var td_el = item.Elements("field").FirstOrDefault(f => f.Attribute("prop").Value == "http://fogid.net/o/to-date");
            string res = (fd_el == null ? "" : fd_el.Value) + (td_el == null || string.IsNullOrEmpty(td_el.Value) ? "" : "—" + td_el.Value);
            return res;
        }


        public XElement GetItemById(string id, XElement format)
        {
            return db.GetItemById(id, format);
        }
        public XElement GetItemByIdBasic(string id, bool addinverse)
        {
            return db.GetItemByIdBasic(id, addinverse);
        }
        public IEnumerable<XElement> SearchByName(string ss)
        {
            var query = db.SearchByName(ss)
                ;
            return query;
        }

        public IEnumerable<XElement> GetCollectionPath(string id)
        {
            //return _getCollectionPath(id);
            System.Collections.Generic.Stack<XElement> stack = new Stack<XElement>();
            XElement node = GetItemByIdBasic(id, false);
            if (node == null) return Enumerable.Empty<XElement>();
            stack.Push(node);
            bool ok = GetCP(id, stack);
            if (!ok) return Enumerable.Empty<XElement>();
            int n = stack.Count();
            // Уберем первый и последний
            var query = stack.Skip(1).Take(n - 2).ToArray();
            return query;
        }
        // В стеке накоплены элементы пути, следующие за id. Последним является узел с id
        private bool GetCP(string id, Stack<XElement> stack)
        {
            if (id == funds_id) return true;
            XElement tree = GetItemById(id, formattoparentcollection);
            if (tree == null) return false;
            foreach (var n1 in tree.Elements("inverse"))
            {
                var n2 = n1.Element("record"); if (n2 == null) return false;
                var n3 = n2.Element("direct"); if (n3 == null) return false;
                var node = n3.Element("record"); if (node == null) return false;
                string nid = node.Attribute("id").Value;
                stack.Push(node);
                bool ok = GetCP(nid, stack);
                if (ok) return true;
                stack.Pop();
            }
            return false;
        }
        private XElement formattoparentcollection =
new XElement("record",
new XElement("inverse", new XAttribute("prop", "http://fogid.net/o/collection-item"),
    new XElement("record",
        new XElement("direct", new XAttribute("prop", "http://fogid.net/o/in-collection"),
            new XElement("record", new XAttribute("type", "http://fogid.net/o/collection"),
                new XElement("field", new XAttribute("prop", "http://fogid.net/o/name")))))));

    }
    public class SCompare : IComparer<string>
    {
        public int Compare(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 0;
            if (string.IsNullOrEmpty(s1)) return 1;
            if (string.IsNullOrEmpty(s2)) return -1;
            return s1.CompareTo(s2);
        }
        //public static SCompare comparer = new SCompare();
    }

}

