//	Copyright © 2024, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
//	Author: Pierre ARNAUD, Maintainer: Pierre ARNAUD

using System.Text;

namespace Epsitec.Utfify;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine ("Usage: utfify <directory>");
            return;
        }

        var utf8 = new UTF8Encoding(false, true);
        string[] patterns =
        [
            "*.cs", "*.razor", "*.csproj",
            "*.xml", "*.xsl",
            "*.json", "*.md",
            "*.html", "*.htm", "*.css", "*.js", "*.ts"
        ];

        args.SelectMany (root
            => patterns.SelectMany (pattern
                => Directory.GetFiles (root, pattern, SearchOption.AllDirectories)))
            .Where (path => path.Contains ("\\.git\\") == false)
            .OrderBy (x => x)
            .AsParallel ()
            .ForAll (file => Process (file, utf8));
    }

    private static void Process(string path, UTF8Encoding utf8)
    {
        var data = File.ReadAllBytes (path);
        
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            //  Found UTF-8 BOM
            return;
        }

        //  Detect encoding of a text file which will either be UTF-8
        //  or Latin1 (ISO-8859-1).

        try
        {
            utf8.GetString (data);

            //  OK, this is implicitly a UTF-8 file, but without BOM.
            //  Leave it as is.
        }
        catch (DecoderFallbackException)
        {
            //  This is not an UTF-8 file... assume it is a Latin1 file
            //  and convert it to UTF-8, enforcing a BOM.

            var text = Iso88591.GetString (data);
            if (text.Length != data.Length)
            {
                throw new InvalidOperationException ();
            }

            var creationTime = File.GetCreationTimeUtc (path);

            data = Utf8WithBom.GetBytes (text);

            File.WriteAllBytes (path, data);
            File.SetCreationTimeUtc (path, creationTime);

            Console.WriteLine (path);
        }
    }

    private static readonly Encoding Iso88591 = Encoding.GetEncoding ("ISO-8859-1");
    private static readonly Encoding Utf8WithBom = new UTF8Encoding (true);
}