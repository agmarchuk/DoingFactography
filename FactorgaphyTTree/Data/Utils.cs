using FactorgaphyTTree.Shared;
using MagBlazor.Data;

namespace FactorgaphyTTree.Data
{
    public static class Utils
    {
        // to utills

        private static bool showLabels = true;
        private static string lang = MainLayout.defaultLanguage;

        public static IFDataService db;

        public static IFDataService Db
        {
            get
            {
                return db;
            }
        }

        public static void InitUtils(IFDataService _db)
        {
            db = _db;
        }

        //[Inject]
        //public static IFDataService db { get; set; }

        public static string GetOntLabel(string input) // lang?
        {
            return showLabels == true ? db.ontology.LabelOfOnto(input) : input;
        }

        public static string? GetLangText(TGroup input, string? enumSpecificatior = null) // add fallback lang
        {
            if (enumSpecificatior is not null) // for enumerations
            {
                var text = ((Texts)input).Values.FirstOrDefault()?.Text;
                return String.IsNullOrEmpty(text) ? "" : db.ontology.EnumValue(enumSpecificatior, ((Texts)input).Values.FirstOrDefault()?.Text, lang);
            }
            if (input == null)
            {
                return null;
            }
            //if (langText == null)
            //{
            //    langText = ((Texts)input).Values.FirstOrDefault(); // 1) Try default user lang
            //}
            var langText = ((Texts)input).Values.FirstOrDefault(val => val.Lang == lang || String.IsNullOrEmpty(val.Lang)); // 2) Try default or empty lang
            if (langText == null)
            {
                langText = ((Texts)input).Values.FirstOrDefault(); // 3) First available lang
            }
            return langText?.Text;
        }

        public static string? GetName(TTree ttree)
        {
            return GetLangText(ttree.Groups.FirstOrDefault(gr => gr.Pred == MainLayout.defaultNameProp));
        }
    }
}
