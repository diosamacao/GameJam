using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace Emergence.Level01
{
    // A deterministic reveal schedule; pause markers never reach the displayed text.
    public sealed class DialogueRevealTimeline
    {
        static readonly Regex Pause=new Regex(@"<pause=([0-9]+(?:\.[0-9]+)?)>");
        readonly List<int> ends=new List<int>();
        readonly List<double> times=new List<double>();
        public string Text {get;private set;}
        public double Duration {get;private set;}
        public DialogueRevealTimeline(string source,float charactersPerSecond)
        {
            source=source??"";var text=new StringBuilder();double time=0;int offset=0;
            double interval=1.0/System.Math.Max(1,charactersPerSecond);
            foreach(Match match in Pause.Matches(source)){
                double seconds;
                if(!double.TryParse(match.Groups[1].Value,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out seconds) || double.IsInfinity(seconds))continue;
                Append(source.Substring(offset,match.Index-offset),text,interval,ref time);
                time+=seconds;offset=match.Index+match.Length;
            }
            Append(source.Substring(offset),text,interval,ref time);Text=text.ToString();Duration=time;
        }
        void Append(string value,StringBuilder text,double interval,ref double time)
        {
            var elements=StringInfo.GetTextElementEnumerator(value);
            while(elements.MoveNext()){text.Append(elements.GetTextElement());time+=interval;ends.Add(text.Length);times.Add(time);}
        }
        public string VisibleAt(double elapsed)
        {
            int low=0,high=times.Count;
            while(low<high){int mid=(low+high)/2;if(times[mid]<=elapsed)low=mid+1;else high=mid;}
            return low==0?"":Text.Substring(0,ends[low-1]);
        }
    }
}
