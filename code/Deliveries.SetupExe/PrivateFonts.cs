using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Text;

namespace SetupExe
{
    class PrivateFonts
    {
        [DllImport("Gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr AddFontMemResourceEx(IntPtr pbFont, int cbFont, int pdv, ref int pcFonts);

        public System.Drawing.Text.PrivateFontCollection GetFont(string[] FontResource)
        {
            //Get the namespace of the application    
            //string NameSpc = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.ToString();
            string NameSpc = "SetupExe";
            Stream FntStrm;
            PrivateFontCollection FntFC = new PrivateFontCollection();
            int i;
            for (i = 0; i <= FontResource.GetUpperBound(0); i++)
            {
                //Get the resource stream area where the font is located
                FntStrm = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(NameSpc + "." + FontResource[i]);
                //Load the font off the stream into a byte array
                byte[] ByteStrm = new byte[(int)FntStrm.Length + 1];
                FntStrm.Read(ByteStrm, 0, Convert.ToInt32((int)FntStrm.Length));
                //Allocate some memory on the global heap
                IntPtr FntPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(typeof(byte)) * ByteStrm.Length);
                //Copy the byte array holding the font into the allocated memory.
                System.Runtime.InteropServices.Marshal.Copy(ByteStrm, 0, FntPtr, ByteStrm.Length);
                //Add the font to the PrivateFontCollection
                FntFC.AddMemoryFont(FntPtr, ByteStrm.Length);
                Int32 pcFonts;
                pcFonts = 1;
                AddFontMemResourceEx(FntPtr, ByteStrm.Length, 0, ref pcFonts);
                //Free the memory
                System.Runtime.InteropServices.Marshal.FreeHGlobal(FntPtr);
            }
            return FntFC;
        }
    }
}
