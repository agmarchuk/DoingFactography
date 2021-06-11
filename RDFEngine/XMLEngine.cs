﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RDFEngine
{
    public class XMLEngine : IEngine
    {
        // Основным объектом будет XML-представление RDF-данных
        private XElement rdf;

        // Константы для удобства
        static XName rdfabout = XName.Get("about", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");
        static XName rdfresource = XName.Get("resource", "http://www.w3.org/1999/02/22-rdf-syntax-ns#");

        // Констрируется пустая база данных
        public XMLEngine()
        {
            Clear();
        }

        // Пустая база данных состоит из элемента rdf:RDF и нужных для удобного вывода пространств имен. Для начала, используется только стандарт
        public void Clear()
        {
            rdf = XElement.Parse(
@"<?xml version='1.0' encoding='utf-8'?>
<rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
</rdf:RDF>");
        }

        // Загрузка части базы данных, таких загрузок может быть несколько
        public void Load(IEnumerable<XElement> records)
        {
            rdf.Add(records);
        }

        // Дополнительные структуры к rdf. Словарь элементов и словарь ссылочных элементов
        private Dictionary<string, XElement> recordsById;
        private Dictionary<string, XElement[]> subelementsByResource;


        public void Build()
        {
            // Предполагается, что элементы rdf загружены, базу данных надо "доделать" и сформировать дополнительные структуры
            recordsById = new Dictionary<string, XElement>();
            // Для размещения ссылок на ресурсы использую более удобное решение, а потом преобразую в subelementsByResource
            Dictionary<string, List<XElement>> subelements = new Dictionary<string, List<XElement>>();
            
            // Сканирую записи и "разбрасываю" ссылки по словарям
            foreach (XElement rec in rdf.Elements())
            {
                recordsById.Add(rec.Attribute(rdfabout).Value, rec);
                foreach (XElement sub in rec.Elements())
                {
                    string resource = sub.Attribute(rdfresource)?.Value;
                    if (resource == null) continue;
                    if (subelements.ContainsKey(resource))
                    {
                        subelements[resource].Add(sub);
                    }
                    else
                    {
                        subelements.Add(resource, new List<XElement>(new XElement[] { sub }));
                    }
                }
            }
            // Теперь для экономии перекину subelements в subelementsByResource
            subelementsByResource = subelements.Select(x => new { x.Key, x.Value })
                .ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
        }

        // Сначала сделаем преобразователь записи в промежуточное представление
        private XElement ConvertToIntermediate(XElement rec)
        {
            return new XElement("record",
                new XAttribute("id", rec.Attribute(rdfabout).Value),
                new XAttribute("type", rec.Name.NamespaceName + rec.Name.LocalName),
                rec.Elements()
                    .Select(el =>
                    {
                        string prop = el.Name.NamespaceName + el.Name.LocalName;
                        XAttribute resource = el.Attribute(rdfresource);
                        if (resource != null)
                        {
                            return new XElement("direct", new XAttribute("prop", prop),
                                new XElement("record", new XAttribute("id", resource.Value)));
                        }
                        else
                        {
                            XAttribute xlang = el.Attribute("{http://www.w3.org/XML/1998/namespace}lang");
                            return new XElement("field", new XAttribute("prop", prop), 
                                xlang == null ? null : new XAttribute(xlang),
                                el.Value);
                        }
                    }));
        }
        public IEnumerable<XElement> Search(string sample)
        {
            sample = sample.ToLower(); // Сравнивать будем в нижнем регистре
            var query = rdf.Elements()
                .Where(r => r.Elements("name").Any(f => f.Value.StartsWith(sample)))
                // преобразуем в промежуточное представление
                .Select(r => new XElement("record", 
                    new XAttribute("id", r.Attribute(rdfabout).Value),
                    new XAttribute("type", r.Name.Namespace + r.Name.LocalName),
                    null));
            return query;
        }
        

        public XElement GetRecord(string id, bool addinverse)
        {
            throw new NotImplementedException();
        }


    }
}