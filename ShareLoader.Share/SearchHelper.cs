namespace ShareLoader.Share;

using System.Text.RegularExpressions;

public class SearchHelper
{
    public static void GetSearch(CheckViewModel model)
    {
        Match m = new Regex(@"[a-z \.\\\/\(\-]((19|20)[0-9]{2})[a-z \.\\\/\)\-]").Match(model.Name);

        if (m.Success)
        {
            string search = model.Name.Substring(0, model.Name.LastIndexOf(m.Groups[1].Value) - 1).Replace('.', ' ');
            if (search.EndsWith(' '))
                search = search.Substring(0, search.ToString().Length);
            model.Search = search;
            model.Type = DownloadType.Movie;
        }
        else
        {
            m = new Regex("(English|German)").Match(model.Name);
            if (m.Success)
            {
                string search = model.Name.Substring(0, model.Name.LastIndexOf(m.Value)).Replace('.', ' ');
                if (search.EndsWith(' '))
                    search = search.Substring(0, search.ToString().Length - 1);
                model.Search = search;
                model.Type = DownloadType.Movie;
            }
        }

        m = new Regex(@"S([0-9]{1,2})[a-z \.\\\/\)\-]").Match(model.Name);
        if (m.Success)
        {
            string search = model.Name.Substring(0, model.Name.LastIndexOf(m.Value)).Replace('.', ' ');
            if (search.EndsWith(' '))
                search = search.Substring(0, search.ToString().Length - 1);
            model.Search = search;
            model.Type = DownloadType.Soap;
        }
    }
}