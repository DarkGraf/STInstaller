using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace InstallerStudio.Utils
{
  static class IconHelper
  {
    [DllImport("shell32.dll")]
    private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phIconLarge, IntPtr[] phIconSmall, int nIcons);

    [DllImport("user32.dll")]
    private static extern int DestroyIcon(IntPtr hIcon);

    public static Icon ExtractIcon(string file, int index, bool large)
    {
      int readCount = 0;
      IntPtr[] hDummy = new IntPtr[1] { IntPtr.Zero };
      IntPtr[] hIcon = new IntPtr[1] { IntPtr.Zero };
      Icon extractedIcon = null;

      try
      {
        if (large)
        {
          readCount = ExtractIconEx(file, index, hIcon, hDummy, 1);
        }
        else
        {
          readCount = ExtractIconEx(file, index, hDummy, hIcon, 1);
        }

        if (readCount > 0 && hIcon[0] != IntPtr.Zero)
        {
          extractedIcon = (Icon)Icon.FromHandle(hIcon[0]).Clone();
        }

        return extractedIcon;
      }
      catch (Exception ex)
      {
        throw new Exception(string.Format("Не удалось извлечь пиктограмму из {0} ({1}).", file, ex.Message));
      }
      finally
      {
        foreach (IntPtr ptr in hIcon)
        {
          if (ptr != IntPtr.Zero)
          {
            DestroyIcon(ptr);
          }
        }

        foreach (IntPtr ptr in hDummy)
        {
          if (ptr != IntPtr.Zero)
          {
            DestroyIcon(ptr);
          }
        }
      }
    }
  }
}
