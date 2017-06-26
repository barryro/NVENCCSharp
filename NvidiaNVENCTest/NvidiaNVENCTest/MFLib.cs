using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaFoundation;
using MediaFoundation.Misc;
using System.Runtime.InteropServices;

namespace NvidiaNVENCTest
{
    class MFLib
    {
        public static byte[] GetDataFromMediaSample(IMFSample sample)
        {
            IMFMediaBuffer pMediaBuffer = null;
            IntPtr pBuffer;
            int maxLen = 0, curLen = 0;

            //HResult hr = sample.LockStore();
            HResult hr = sample.ConvertToContiguousBuffer(out pMediaBuffer);
            MFError.ThrowExceptionForHR(hr);
            try
            {
                hr = pMediaBuffer.Lock(out pBuffer, out maxLen, out curLen);
                MFError.ThrowExceptionForHR(hr);

                try
                {
                    byte[] data = new byte[curLen];
                    Marshal.Copy(pBuffer, data, 0, curLen);
                    return data;
                }
                finally
                {
                    hr = pMediaBuffer.Unlock();
                }
            }
            finally
            {
                //hr = sample.UnlockStore();
                COMBase.SafeRelease(pMediaBuffer);
            }
        }
    }
}
