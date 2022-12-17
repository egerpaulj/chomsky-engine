//      Microservice Message Exchange Libraries for .Net C#                                                                                                                                       
//      Copyright (C) 2022  Paul Eger                                                                                                                                                                     

//      This program is free software: you can redistribute it and/or modify                                                                                                                                          
//      it under the terms of the GNU General Public License as published by                                                                                                                                          
//      the Free Software Foundation, either version 3 of the License, or                                                                                                                                             
//      (at your option) any later version.                                                                                                                                                                           

//      This program is distributed in the hope that it will be useful,                                                                                                                                               
//      but WITHOUT ANY WARRANTY; without even the implied warranty of                                                                                                                                                
//      MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                                                                                                                                                 
//      GNU General Public License for more details.                                                                                                                                                                  

//      You should have received a copy of the GNU General Public License                                                                                                                                             
//      along with this program.  If not, see <https://www.gnu.org/licenses/>.
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Crawler.Core.Parser.DocumentParts;
using Crawler.Core.Parser.DocumentParts.Serialilzation;
using LanguageExt;

namespace Crawler.Core.Parser
{
    public class Document
    {
        public Option<XDocument> XmlDocument { get; set; }

        
        [JsonConverter(typeof(BaseClassConverter))]
        public Option<DocumentPart> RequestDocumentPart { get; set; }

        public Option<bool> DownloadContent { get; set; }

        public string GetBriefSummary()
        {
            var isDownloadContent = DownloadContent.Match(d => d, () => false);
            var parseDocument = RequestDocumentPart.Match(d => d.GetBriefSummary(), () => string.Empty);

            return $"DOCUMENT (DownloadContent:{isDownloadContent}): \n Summary: {parseDocument}";
        }
    }
}