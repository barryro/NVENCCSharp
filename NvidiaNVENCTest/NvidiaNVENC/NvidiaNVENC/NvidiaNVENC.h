////////////////////////////////////////////////////////////////////////////
// Hereby to notify that
//  "This library contains source code provided by NVIDIA Corporation."
//														2017/07/17 Barry
////////////////////////////////////////////////////////////////////////////

// NvidiaNVENC.h
#pragma once

#ifndef _NVIDIANVENC_H_
#define	_NVIDIANVENC_H_

//#ifdef NVIDIANVENC_EXPORTS
//#define NVIDIANVENC_API __declspec(dllexport)
//#else
//#define NVIDIANVENC_API __declspec(dllimport)
//#endif

//#if defined(NV_WINDOWS)
#include <d3d9.h>
#include <d3d10_1.h>
#include <d3d11.h>
#pragma warning(disable : 4996)
//#endif

#include "stdafx.h"
#include "../common/inc/NvHWEncoder.h"

#define MAX_ENCODE_QUEUE 32
#define FRAME_QUEUE 240
#define NUM_OF_MVHINTS_PER_BLOCK8x8   4
#define NUM_OF_MVHINTS_PER_BLOCK8x16  2
#define NUM_OF_MVHINTS_PER_BLOCK16x8  2
#define NUM_OF_MVHINTS_PER_BLOCK16x16 1

enum
{
	PARTITION_TYPE_16x16,
	PARTITION_TYPE_8x8,
	PARTITION_TYPE_16x8,
	PARTITION_TYPE_8x16
};
#define SET_VER(configStruct, type) {configStruct.version = type##_VER;}

template<class T>
class CNvQueue {
	T** m_pBuffer;
	unsigned int m_uSize;
	unsigned int m_uPendingCount;
	unsigned int m_uAvailableIdx;
	unsigned int m_uPendingndex;
public:
	CNvQueue() : m_pBuffer(NULL), m_uSize(0), m_uPendingCount(0), m_uAvailableIdx(0),
		m_uPendingndex(0)
	{
	}

	~CNvQueue()
	{
		delete[] m_pBuffer;
	}

	bool Initialize(T *pItems, unsigned int uSize)
	{
		m_uSize = uSize;
		m_uPendingCount = 0;
		m_uAvailableIdx = 0;
		m_uPendingndex = 0;
		m_pBuffer = new T *[m_uSize];
		for (unsigned int i = 0; i < m_uSize; i++)
		{
			m_pBuffer[i] = &pItems[i];
		}
		return true;
	}


	T * GetAvailable()
	{
		T *pItem = NULL;
		if (m_uPendingCount == m_uSize)
		{
			return NULL;
		}
		pItem = m_pBuffer[m_uAvailableIdx];
		m_uAvailableIdx = (m_uAvailableIdx + 1) % m_uSize;
		m_uPendingCount += 1;
		return pItem;
	}

	T* GetPending()
	{
		if (m_uPendingCount == 0)
		{
			return NULL;
		}

		T *pItem = m_pBuffer[m_uPendingndex];
		m_uPendingndex = (m_uPendingndex + 1) % m_uSize;
		m_uPendingCount -= 1;
		return pItem;
	}
};

typedef struct _EncodeFrameConfig
{
	uint8_t  *yuv[3];
	uint32_t stride[3];
	uint32_t width;
	uint32_t height;
	int8_t *qpDeltaMapArray;
	uint32_t qpDeltaMapArraySize;
	NVENC_EXTERNAL_ME_HINT *meExternalHints;
	NVENC_EXTERNAL_ME_HINT_COUNTS_PER_BLOCKTYPE meHintCountsPerBlock[1];
}EncodeFrameConfig;

typedef enum
{
	NV_ENC_DX9 = 0,
	NV_ENC_DX11 = 1,
	NV_ENC_CUDA = 2,
	NV_ENC_DX10 = 3,
} NvEncodeDeviceType;

class CNvEncoder
{
public:
	CNvEncoder();
	virtual ~CNvEncoder();

	int                                                  EncodeMain(int argc, char **argv);
	
	//20170622
	bool GetLibraryFromManaged(HINSTANCE intPtr, MYPROC proc);
	bool InitilaizeNvEncoder(NV_ENC_BUFFER_FORMAT inputFomat, int width, int height, int frameRate = 30, int bitrate = 5000000, int rcMode = NV_ENC_PARAMS_RC_CONSTQP);
	NVENCSTATUS ProcessData(byte *inputData, byte **outputData, uint32_t &sizeOfOutput, 
							bool &isKey, uint64_t timestamp, uint64_t duration, uint64_t &outTimeStamp);

	NVENCSTATUS EncodeFrame2(EncodeFrameConfig *pEncodeFrame, bool bFlush, uint32_t width, uint32_t height, 
			NV_ENC_LOCK_BITSTREAM &lockBitstreamData, EncodeBuffer *pEncodeBuffer, uint64_t timestamp = 0, uint64_t duration = 0);
	NVENCSTATUS EncodeFrame3(EncodeFrameConfig *pEncodeFrame, bool bFlush, uint64_t inputTimestamp, uint64_t inputDuration, byte **outputData, uint32_t &outputDataSize, uint64_t &outputTimeStamp, bool &isKey);
	
	bool EndOfProcessData();

	NVENCSTATUS FlushEncoder2(NV_ENC_LOCK_BITSTREAM &lockBitstreamData, bool &stop);
	NVENCSTATUS FlushEncoder3(byte **outputData, uint32_t &outputDataSize, uint64_t &outputTimeStamp, bool &isKey, bool &stop);

	NVENCSTATUS NvEncFlushEncoderQueue();
	bool FinalizeEncoder();
protected:
	CNvHWEncoder                                        *m_pNvHWEncoder;
	uint32_t                                             m_uEncodeBufferCount;
	uint32_t                                             m_uPicStruct;
	void*                                                m_pDevice;
	//20170628
	bool m_firstFrame;
#if defined(NV_WINDOWS)
	IDirect3D9                                          *m_pD3D;
#endif

	CUcontext                                            m_cuContext;
	EncodeConfig                                         m_stEncoderInput;
	EncodeBuffer                                         m_stEncodeBuffer[MAX_ENCODE_QUEUE];
	MotionEstimationBuffer                               m_stMVBuffer[MAX_ENCODE_QUEUE];
	CNvQueue<EncodeBuffer>                               m_EncodeBufferQueue;
	CNvQueue<MotionEstimationBuffer>                     m_MVBufferQueue;
	EncodeOutputBuffer                                   m_stEOSOutputBfr;

	//20170623
	EncodeConfig m_encodeConfig;
	FILE *m_pFile;
protected:
	NVENCSTATUS                                          Deinitialize(uint32_t devicetype);
	NVENCSTATUS                                          EncodeFrame(EncodeFrameConfig *pEncodeFrame, bool bFlush = false, uint32_t width = 0, uint32_t height = 0);
	NVENCSTATUS                                          InitD3D9(uint32_t deviceID = 0);
	NVENCSTATUS                                          InitD3D11(uint32_t deviceID = 0);
	NVENCSTATUS                                          InitD3D10(uint32_t deviceID = 0);
	NVENCSTATUS                                          InitCuda(uint32_t deviceID = 0);
	NVENCSTATUS                                          AllocateIOBuffers(uint32_t uInputWidth, uint32_t uInputHeight, NV_ENC_BUFFER_FORMAT inputFormat);
	NVENCSTATUS                                          AllocateMVIOBuffers(uint32_t uInputWidth, uint32_t uInputHeight, NV_ENC_BUFFER_FORMAT inputFormat);
	NVENCSTATUS                                          ReleaseIOBuffers();
	NVENCSTATUS                                          ReleaseMVIOBuffers();
	unsigned char*                                       LockInputBuffer(void * hInputSurface, uint32_t *pLockedPitch);
	NVENCSTATUS                                          FlushEncoder();
	void                                                 FlushMVOutputBuffer();
	NVENCSTATUS                                          RunMotionEstimationOnly(MEOnlyConfig *pMEOnly, bool bFlush);
};

// NVEncodeAPI entry point
typedef NVENCSTATUS(NVENCAPI *MYPROC)(NV_ENCODE_API_FUNCTION_LIST*);

///20170626
///
/// NvidiaNVENC is a class to link between CNvEncoder and C# 
///
using namespace System;
using namespace System::Runtime::InteropServices;

namespace NvidiaNVENC {

	////For C++ dll to C# testing
	//public ref class Arithmetics
	//{
	//public:
	//	// TODO: 在此加入這個類別的方法。
	//	Arithmetics(){};
	//	double Add(double x, double y);
	//	double Subtract(double a, double b);
	//	double Multiply(double x, double y);
	//	double Divide(double a, double b);
	//};

	public ref class NvEncoder
	{
	public:
		NvEncoder();
		~NvEncoder();
		int EncodeMain();
		bool GetLibraryFromManaged(IntPtr intPtr, IntPtr proc);
		bool InitializeNvEncoder(Guid inputFormat, int width, int height, int frameRate, int bitrate, int rcMode);
		int ProcessData(array<System::Byte> ^inputData, int width, int height, [Out] array<System::Byte> ^%outputData, [Out] bool %isKey, uint64_t timestamp, uint64_t duration, [Out] uint64_t %outTimeStamp);
		bool StopProcessData();
		bool FinalizeEncoder();

	private:
		//void *m_nvEncoder;
		CNvEncoder *m_nvEncoder;
	};

	NvEncoder::NvEncoder()
	{
		m_nvEncoder = new CNvEncoder();
	}

	NvEncoder::~NvEncoder()
	{
		m_nvEncoder->~CNvEncoder();
	}
}
#endif
