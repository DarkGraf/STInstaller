using System;
using System.Runtime.Serialization;

namespace InstallerStudio.WixElements
{
  [DataContract(Namespace = StringResources.Namespace)]
  class AppVersion
  {
    public AppVersion()
    {
      Major = 0;
      Minor = 0;
      Build = 0;
      Revision = 0;
    }

    public AppVersion(int major, int minor, int build, int revision)
    {
      Major = major;
      Minor = minor;
      Build = build;
      Revision = revision;
    }

    public AppVersion(string version)
    {
      Parse(version);
    }

    [DataMember]
    public int Major { get; set; }
    [DataMember]
    public int Minor { get; set; }
    [DataMember]
    public int Build { get; set; }
    [DataMember]
    public int Revision { get; set; }

    private void Parse(string version)
    {
      if (version == null)
        throw new ArgumentNullException();

      string[] strArray = version.Split(new char[] { '.' });

      if (strArray.Length != 4)
        throw new ArgumentException();

      for (int i = 0; i < strArray.Length; i++)
      {
        int d;
        if (!int.TryParse(strArray[i], out d))
          throw new ArgumentException();

        switch (i)
        {
          case 0:
            Major = d;
            break;
          case 1:
            Minor = d;
            break;
          case 2:
            Build = d;
            break;
          case 3:
            Revision = d;
            break;
        }
      }
    }

    public override string ToString()
    {
      return string.Format("{0}.{1}.{2}.{3}", Major, Minor, Build, Revision);
    }

    public static implicit operator AppVersion(string versionString)
    {
      return new AppVersion(versionString);
    }

    public static implicit operator string(AppVersion version)
    {
      return version.ToString();
    }

    public override bool Equals(object obj)
    {
      AppVersion other = obj as AppVersion;
      return other != null && Major == other.Major && Minor == other.Minor &&
        Build == other.Build && Revision == other.Revision;
    }

    public override int GetHashCode()
    {
      return ToString().GetHashCode();
    }
  }
}
