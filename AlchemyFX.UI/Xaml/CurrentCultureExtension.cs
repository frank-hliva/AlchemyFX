using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace AlchemyFX.UI.Xaml
{
    public class CurrentCultureExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
        }
    }
}
