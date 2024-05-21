using System.Collections;

namespace FastaHelperLib_Test;

public class ValidMultiFastaDataWithComments : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[]
        {
            "#comment 1\n# comment 2\n>header1\nACGTQ\n; comment 3##\n#comment 4\n;\n>header2\nACGTQTR\n>header3\nACGTQTRA",
            new List<(string header, string sequence, List<string> comments)>
            {
                (@"header1", @"ACGTQ", new List<string>(){"#comment 1", "# comment 2"}),
                (@"header2", @"ACGTQTR", new List<string>(){"; comment 3##", "#comment 4",";"}),
                (@"header3", @"ACGTQTRA", new List<string>(){}),
            }
        };
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}