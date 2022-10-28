using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using RDFEngine;

namespace RDFEngine
{
    public class Infobase
    {
        // Движок и онтология
        public static RDFEngine.IEngine engine = null; //TODO: Надо его убрать!!! 
        public static RDFEngine.ROntology rontology = null;
        
        // Параметры приложения
        //public static bool toedit = false;

        // Инициирование приложения, включая чтение и активирование онтологии, пристоединение к базе данных
        private static string _path;
        public static void Init(string path)
        {
            _path = path;

            //OAData.OADB.Init(path);
            //var xel = OAData.OADB.GetItemByIdBasic("newspaper_28156_1997_1", false);
            //var xels = OAData.OADB.SearchByName("марчук александр").ToArray();
            //if (xel != null) Console.WriteLine(xel.ToString());
            //Infobase.engine = new RDFEngine.RXEngine(); // Это новый движок!!!

            Infobase.rontology = new RDFEngine.ROntology(path + "ontology_iis-v13.xml");

            //var trec = new TRecord("famwf1233_1021", rontology);
        }
        public static void Init() { Init(_path); }
        public static void Reload() { OAData.OADB.Close(); Init(); } 

        // Утилита получения имени по записи или null если имени нет   
        public static string GetName(RDFEngine.RRecord rec) =>
    ((RDFEngine.RField)rec.Props.FirstOrDefault(p => p is RDFEngine.RField &&
        (p.Prop == "name" || p.Prop == "http://fogid.net/o/name")))?.Value;

    }
}
