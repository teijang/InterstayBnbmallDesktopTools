using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterstayBnbmallDesktopTools
{
    public class LanguageComboboxItem
    {
        public LanguageComboboxItem(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Text { get; set; }
        public string Value { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }
}
