namespace FastaHelperLib;

public class MultiFastaProcessResult
{
    public List<FastaEntry> ValidFastas { get; set; } = new List<FastaEntry>();
    public List<FastaEntry> InvalidFastas { get; set; } = new List<FastaEntry>();
}