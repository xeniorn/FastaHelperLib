
namespace FastaHelperLib;

public class Protein : IComparable<Protein>
{

    public string Id { get; set; } = String.Empty;
    public string Sequence { get; set; } = String.Empty;

    public bool SameSequenceAs(Protein other)
    {
        if (ReferenceEquals(this,other)) return true;

        return String.Equals(this.Sequence, other.Sequence, StringComparison.OrdinalIgnoreCase);
    }

    public List<Protein> GenerateSplits(int minLen)
    {
        if (this.Sequence.Length < minLen) return new List<Protein>() { (Protein)this.MemberwiseClone() };

        var res = new List<Protein>();
        var count = (int)Math.Ceiling((decimal)this.Sequence.Length / minLen);
        var partWiseLen = this.Sequence.Length / count;
        var extraLen = this.Sequence.Length - partWiseLen * count;

        for (int i = 0; i < count; i++)
        {
            var lenToTake = i < count ? partWiseLen : partWiseLen + extraLen;
            var startIndex = i * partWiseLen;
            var newProt = new Protein()
            {
                Sequence = this.Sequence.Substring(startIndex, lenToTake),
                Id = this.Id + $"[{startIndex+1}-{startIndex+lenToTake}]"
                
            };
            res.Add(newProt);
        }
        return res;
    }

    public int CompareTo(Protein? other)
    {
        if (other is null) return -1;
        if (this.Equals(other)) return 0;

        if (this.Sequence.Length > other.Sequence.Length) return -1;
        if (this.Sequence.Length < other.Sequence.Length) return 1;

        //equal length
        return String.Compare(this.Sequence, other.Sequence, StringComparison.OrdinalIgnoreCase);
    }

}