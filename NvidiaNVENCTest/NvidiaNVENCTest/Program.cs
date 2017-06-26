using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NvidiaNVENC;
using System.Runtime.InteropServices;
using MediaFoundation;
using MediaFoundation.ReadWrite;
using MediaFoundation.Misc;

namespace NvidiaNVENCTest
{
    class Program
    {
        //[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        //static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string strLibraryName);

        //[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        //static extern Int32 FreeLibrary(IntPtr hModule);

        //[DllImport("Kernel32.dll", CallingConvention = CallingConvention.StdCall)]
        //static extern IntPtr GetProcAddress(IntPtr hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        public static readonly int MF_API_VERSION = 0x0070;
        public static readonly int MF_SDK_VERSION = 0x0002;         // Assume platorm is WINVER >= _WIN32_WINNT_WIN7
        public static readonly int MF_VERSION = MF_SDK_VERSION << 16 | MF_API_VERSION;

        static void Main(string[] args)
        {
            while(true)
            { 
                Console.WriteLine("0: Demo,1: MF");
                string select;
                select = Console.ReadLine();

                if(select == "0")
                {
                    Console.WriteLine("---------Press Enter to intialize NvEncoder ---------");
                    Console.ReadLine();
                    NvEncoder nvEncoder = new NvEncoder();
                    Console.ReadLine();
                    //nvEncoder.GetAPIFromManaged(dllPointer, procAddress);

                    Console.WriteLine("Start EncodeMain");
                    Console.ReadLine();
                    string stop;
                    while (true)
                    {
                        int result = nvEncoder.EncodeMain();
                        Console.WriteLine("Key in 'stop' to stop the while loop");
                        stop = Console.ReadLine();
                        if (stop == "stop")
                            break;
                    }
                    break;
                }
                else if (select == "1")
                {

                    HResult hr = MFExtern.MFStartup(MF_VERSION, MFStartup.Full);

                    IMFSourceReader ppSourceReader;
                    //public extern static HResult MFCreateSourceReaderFromByteStream(IMFByteStream pByteStream, IMFAttributes pAttributes, out IMFSourceReader ppSourceReader);
                    hr = MFExtern.MFCreateSourceReaderFromURL(@"06-13-17-15-58-17-1.mp4", null, out ppSourceReader);

                    //IMFSourceResolver sourceResolver;
                    //MFObjectType pObjectType;
                    //object ppObject;
                    //MFExtern.MFCreateSourceResolver(out sourceResolver);
                    //HResult CreateObjectFromURL(string pwszURL, MFResolution dwFlags, IPropertyStore pProps, out MFObjectType pObjectType, out object ppObject);
                    //hr = sourceResolver.CreateObjectFromURL(@"tulips_yvu420_prog_planar_qcif.yuv", MFResolution.MediaSource, null, out pObjectType, out ppObject);

                    //IMFMediaSource medaiaSource;

                    //hr = MFExtern.MFCreateSourceReaderFromMediaSource();

                    int pdwActualStreamIndex;
                    MF_SOURCE_READER_FLAG pdwStreamFlags = MF_SOURCE_READER_FLAG.None;
                    long pllTimestamp;
                    IMFSample ppSample;
                    Guid temp;

                    IMFMediaType mediaType;
                    MFExtern.MFCreateMediaType(out mediaType);
                    mediaType.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video);
                    mediaType.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MFMediaType.NV12);
                    hr = MFExtern.MFSetAttributeSize(mediaType, MFAttributesClsid.MF_MT_FRAME_SIZE, 1920, 1080);
                    hr = MFExtern.MFSetAttributeRatio(mediaType, MFAttributesClsid.MF_MT_FRAME_RATE, 30, 1);
                    mediaType.SetUINT32(MFAttributesClsid.MF_MT_INTERLACE_MODE, (int)MFVideoInterlaceMode.Progressive);
                    hr = MFExtern.MFSetAttributeRatio(mediaType, MFAttributesClsid.MF_MT_PIXEL_ASPECT_RATIO, 1, 1);

                    hr = ppSourceReader.SetCurrentMediaType(1, null, mediaType);
                    MFError.ThrowExceptionForHR(hr);

                    COMBase.SafeRelease(mediaType);

                    //IMFMediaType tempMediaType;
                    //ppSourceReader.GetCurrentMediaType(1, out tempMediaType);
                    //tempMediaType.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out temp);
                    //COMBase.SafeRelease(tempMediaType);

                    Console.WriteLine("---------Press Enter to intialize NvEncoder ---------");
                    NvEncoder nvEncoder = new NvEncoder();
                    Console.ReadLine();
                    //nvEncoder.GetAPIFromManaged(dllPointer, procAddress);

                    Console.WriteLine("Initialize NvEncoder");

                    bool result = true;
                    result = nvEncoder.InitializeNvEncoder(MFMediaType.NV12, 1920, 1080, 30);
                    //byte[] inputData = null;
                    //byte[] outputData = null;
                    //IntPtr outputData = IntPtr.Zero;

                    //for (int i = 0; i < inputData.Length; i++ )
                    //{
                    //    inputData[i] = 1;
                    //}

                        //IntPtr inputPointer = Marshal.AllocHGlobal(inputData.Length);
                        //Marshal.Copy(inputData, 0, inputPointer, inputData.Length);

                        //IntPtr outputPointer = Marshal.AllocHGlobal(outputData.Length);
                        //Marshal.Copy(outputData, 0, outputPointer, outputData.Length);
                    int processResult = 0;
                    StartByteArrayToFile("Newh264AtProgram.h264");
                        while (result)
                        {
                            byte[] inputData = null;
                            byte[] outputData = null;

                            //HResult ReadSample(int dwStreamIndex, MF_SOURCE_READER_CONTROL_FLAG dwControlFlags, out int pdwActualStreamIndex, out MF_SOURCE_READER_FLAG pdwStreamFlags, out long pllTimestamp, out IMFSample ppSample);
                            ppSourceReader.ReadSample(1, MF_SOURCE_READER_CONTROL_FLAG.None, out pdwActualStreamIndex, out pdwStreamFlags, out pllTimestamp, out ppSample);
                            if (pdwStreamFlags == MF_SOURCE_READER_FLAG.EndOfStream)
                            {
                                Console.WriteLine("End of stream");
                                nvEncoder.EndOfProcessData();
                                
                                while(true)
                                { 
                                    processResult = nvEncoder.ProcessData(null, 1920, 1088, out outputData);
                                    if (outputData != null)
                                    {
                                        ByteArrayToFile(outputData);
                                    }
                                    if (processResult == -1)
                                        break;
                                }
                                StopByteArrayToFile();
                                break;
                            }

                            if (ppSample != null)
                            { 

                                inputData = MFLib.GetDataFromMediaSample(ppSample);

                                nvEncoder.ProcessData(inputData, 1920, 1088, out outputData);

                                if (outputData != null)
                                {
                                    ByteArrayToFile(outputData);
                                }

                                // Call unmanaged code
                                //Marshal.FreeHGlobal(inputPointer);
                                //Marshal.FreeHGlobal(outputPointer);

                                COMBase.SafeRelease(ppSample);

                           
                            }
                            System.Threading.Thread.Sleep(100);
                        }

                    //Release COM objects
                    COMBase.SafeRelease(ppSourceReader);
                    MFExtern.MFShutdown();

                    Console.WriteLine("Shutdown MF");
                    Console.ReadLine();
                    break;
                }
            }

            //double x = 1;
            //double y = 2;
            //double result = 0;
            //Arithmetics ar = new Arithmetics();
            //result = ar.Add(x, y);
            //Console.WriteLine("x = {0}, y = {1}, result = {2}", x, y, result);
            //Console.ReadLine();
 
            //Console.WriteLine("---------Press Enter to get API---------");
            //Console.ReadLine();
            //IntPtr dllPointer = LoadLibrary("nvEncodeAPI.dll");
            //IntPtr procAddress = GetProcAddress(dllPointer, "NvEncodeAPICreateInstance");

          
        }

        static System.IO.FileStream sFileStream = null;
        static bool sIsInitialize = false;
        static bool sIsStartRecording = false;
        static private bool ByteArrayToFile(byte[] byteArray)
        {
            try
            {
                // Writes a block of bytes to this stream using data from
                // a byte array.
                if (sFileStream != null)
                    sFileStream.Write(byteArray, 0, byteArray.Length);

                return true;
            }
            catch (Exception e)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}",
                                  e.ToString());
            }

            // error occured, return false
            return false;
        }

        static public bool StopByteArrayToFile()
        {
            try
            {
                sIsInitialize = false;
                sIsStartRecording = false;
                //StopCapturing();
                sFileStream.Close();
                sFileStream = null;

                return true;
            }
            catch
            {

            }
            // error occured, return false
            return false;
        }

        static public bool StartByteArrayToFile(string fileName)
        {
            try
            {
                // Open file for reading
                if (!sIsInitialize)
                {
                    sFileStream = new System.IO.FileStream(fileName, System.IO.FileMode.Create,
                                               System.IO.FileAccess.Write);
                    sIsInitialize = true;
                    sIsStartRecording = true;

                    return true;
                }
            }
            catch (Exception e)
            {
                // Error
                Console.WriteLine("Exception caught in process: {0}",
                                  e.ToString());
            }

            return false;
        }
    }
}
