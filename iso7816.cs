using System;
using System.Threading;	// Sleep



class ISO7816 : RS232Interface{
	// Attribute ==========
	//private List<byte> recvData = new List<byte>();
	//private byte[] recvBuf;
	//private Int32 nBufLength;

	// Method ==========
	public ISO7816(){
		//recvBuf = new byte[4096];
	}



	/*
	 * Method name	: PowerOn
	 * Return		: Data + Length
	 * Remarks		: Data format
	 *				Offset	Field			Size
	 *				0		bMessageType	1
	 *				1		dwLength		4
	 *				5		bSlot			1
	 *				6		bSeq			1
	 *				7		bPowerSelect	1
	 *				8		abRFU			2
	 */
	public RS232Data PowerOn(){
		byte[] poweron = new byte[] {0x62, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x60};
		RS232Data recvData = new RS232Data();
		Int32 length;

		// Send data
		poweron[10] = 0x00;
		poweron[10] = MakeLRC(poweron, 10);
		this.Write(poweron, Buffer.ByteLength(poweron));

		// Receive data
		length = 0;
		recvData.ClearBuffer();

		Thread.Sleep(10);
		length = this.ReadByte(recvData.data, length, 4096);
		if(length < 5){
			Thread.Sleep(20);
			length += this.ReadByte(recvData.data, length, 4096-length);
		}
		if(length < 5){
			Thread.Sleep(20);
			length += this.ReadByte(recvData.data, length, 4096-length);
		}

		// Check available data
		if(length == 0){
			recvData.nStatus = -1;	// No data
			return recvData;
		}

		recvData.nLength = length;

		// Check MessageType field
		if(recvData.data[0] != 0x80){
			recvData.nStatus = -2;	// MessageType not match
			return recvData;
		}

		// Check LRC
		length = GetDataLength(recvData.data);
		if(recvData.data[recvData.nLength-1] != MakeLRC(recvData.data, 10+length)){
			recvData.nStatus = -3;	// LRC not match
			return recvData;
		}

		return recvData;
	}



	/*
	 * Method name	: PowerOff
	 * Return		: Data + Length
	 * Remarks		: Data format
	 *				Offset	Field			Size
	 *				0		bMessageType	1
	 *				1		dwLength		4
	 *				5		bSlot			1
	 *				6		bSeq			1
	 *				7		abRFU			3
	 */
	public RS232Data PowerOff(){
		byte[] poweroff = new byte[] {0x63, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00};
		RS232Data recvData = new RS232Data();
		Int32 length;

		// Send data
		poweroff[10] = 0x00;
		poweroff[10] = MakeLRC(poweroff, 10);
		this.Write(poweroff, Buffer.ByteLength(poweroff));

		// Receive data
		length = 0;
		recvData.ClearBuffer();

		Thread.Sleep(10);
		length = this.ReadByte(recvData.data, length, 4096);
		if(length < 5){
			Thread.Sleep(20);
			length += this.ReadByte(recvData.data, length, 4096-length);
		}
		if(length < 5){
			Thread.Sleep(20);
			length += this.ReadByte(recvData.data, length, 4096-length);
		}

		// Check available data
		if(length == 0){
			recvData.nStatus = -1;	// No data
			return recvData;
		}

		recvData.nLength = length;

		// Check MessageType field
		if(recvData.data[0] != 0x81){
			recvData.nStatus = -2;	// MessageType not match
			return recvData;
		}

		// Check LRC
		length = GetDataLength(recvData.data);
		if(recvData.data[recvData.nLength-1] != MakeLRC(recvData.data, 10+length)){
			recvData.nStatus = -3;	// LRC not match
			return recvData;
		}

		return recvData;
	}



	/*
	 * Method name	: GetSlotStatus
	 * Return		: Data + Length
	 * Remarks		: Data format
	 *				Offset	Field			Size
	 *				0		bMessageType	1
	 *				1		dwLength		4
	 *				5		bSlot			1
	 *				6		bSeq			1
	 *				7		abRFU			3
	 */
	public RS232Data GetSlotStatus(){
		byte[] getSlotStatus = new byte[] {0x65, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00};
		RS232Data recvData = new RS232Data();
		Int32 length;

		// Send data
		getSlotStatus[10] = 0x00;
		getSlotStatus[10] = MakeLRC(getSlotStatus, 10);
		this.Write(getSlotStatus, Buffer.ByteLength(getSlotStatus));

		// Receive data
		length = 0;
		recvData.ClearBuffer();
		
		Thread.Sleep(10);
		length = this.ReadByte(recvData.data, length, 4096);
		if(length < 5){
			Thread.Sleep(20);
			length += this.ReadByte(recvData.data, length, 4096-length);
		}
		if(length < 5){
			Thread.Sleep(20);
			length += this.ReadByte(recvData.data, length, 4096-length);
		}

		// Check available data
		if(length == 0){
			recvData.nStatus = -1;	// No data
			return recvData;
		}

		recvData.nLength = length;
		
		// Check MessageType field
		if(recvData.data[0] != 0x81){
			recvData.nStatus = -2;	// MessageType not match
			return recvData;
		}
		
		// Check LRC
		length = GetDataLength(recvData.data);
		if(recvData.data[recvData.nLength-1] != MakeLRC(recvData.data, 10+length)){
			recvData.nStatus = -3;	// LRC not match
			return recvData;
		}
		
		return recvData;
	}



	/*
	 * Method name	: XfrBlock
	 * Return		: Data + Length
	 * Remarks		: Data format
	 *				Offset	Field			Size
	 *				0		bMessageType	1
	 *				1		dwLength		4
	 *				5		bSlot			1
	 *				6		bSeq			1
	 *				7		bBWI			1
	 *				8		wLevelParameter	2
	 */
	public RS232Data XfrBlock(byte[] block, Int32 nLength){
		byte[] xfrBlock = new byte[11+nLength];
		RS232Data recvData = new RS232Data();
		Int32 length;

		// Set data
		xfrBlock[0] = 0x6F;	// bMessageType
		SetDataLength(xfrBlock, nLength);
		xfrBlock[5] = 0x00;	// bSlot
		xfrBlock[6] = 0x00;	// bSeq
		xfrBlock[7] = 0x30;	// bBWI
		xfrBlock[8] = 0x00;	// wLevelParameter
		xfrBlock[9] = 0x00;	// wLevelParameter
		Array.Copy(block, 0, xfrBlock, 10, nLength);
		xfrBlock[10+nLength] = MakeLRC(xfrBlock, 10+nLength);	// LRC

		// Send data
		this.Write(xfrBlock, Buffer.ByteLength(xfrBlock));

		// Receive data
		length = 0;
		recvData.ClearBuffer();
		
		Thread.Sleep(10);
		length = this.ReadByte(recvData.data, length, 4096);
		if(length < 5){
			Thread.Sleep(20);
			length += this.ReadByte(recvData.data, length, 4096-length);
		}
		if(length < 5){
			Thread.Sleep(20);
			length += this.ReadByte(recvData.data, length, 4096-length);
		}

		// Check available data
		if(length == 0){
			recvData.nStatus = -1;	// No data
			return recvData;
		}

		recvData.nLength = length;
		
		// Check MessageType field
		if(recvData.data[0] != 0x80){
			recvData.nStatus = -2;	// MessageType not match
			return recvData;
		}

		// Check LRC
		length = GetDataLength(recvData.data);
		if(recvData.data[recvData.nLength-1] != MakeLRC(recvData.data, 10+length)){
			recvData.nStatus = -3;	// LRC not match
			return recvData;
		}

		return recvData;
	}



	/*
	 * Function name	: GetStatusStr
	 * Attr1			: [byte] status
	 * Return			: [string] status message
	 */
	public static string GetStatusStr(byte bStatus){
		string strStatus;
		byte result;
		
		result = (byte)(bStatus & 0x03);
		
		if(result == 0x00){
			strStatus = "An ICC is persent and active (power is on and stable, RST is inactive)\r\n";
		}else if(result == 0x01){
			strStatus = "An ICC is persent and inactive (not activated or shut down by hardware error)\r\n";
		}else if(result == 0x02){
			strStatus = "No ICC is present\r\n";
		}else{
			strStatus = "RFU\r\n";
		}
		
		result = (byte)(bStatus & 0xC0);
		if(result == 0x00){
			strStatus += "Processed without error\r\n";
		}else if(result == 0x40){
			strStatus += "Failed (error code provided by the error register)\r\n";
		}else if(result == 0x80){
			strStatus += "Time Extension is requested\r\n";
		}
		
		return strStatus;
	}



	/*
	 * Function name	: GetErrorStr
	 * Attr1			: [byte] error
	 * Return			: [string] error message
	 */
	public static string GetErrorStr(byte bError){
		if(bError == 0xFF){
			return "CMD_ABORTED(Host aborted the current activity)\r\n";
		}else if(bError == 0xFE){
			return "ICC_MUTE(timed out while talking to the ICC)\r\n";
		}else if(bError == 0xFD){
			return "XFR_PARITY_ERROR(Parity error while talking to the ICC)\r\n";
		}else if(bError == 0xFB){
			return "HW_ERROR(An all inclusive hardware error occurred)\r\n";
		}else if(bError == 0xF8){
			return "BAD_ATR_TS\r\n";
		}else if(bError == 0xF7){
			return "BAD_ATR_TCK\r\n";
		}else if(bError == 0xF6){
			return "ICC_PROTOCOL_NOT_SUPPORTED\r\n";
		}else if(bError == 0xF4){
			return "PROCEDURE_BYTE_CONFLICT\r\n";
		}else if(bError == 0x01){
			return "CMD_FAILED\r\n";
		}else if(bError == 0x00){
			return "CMD_NO_ERROR\r\n";
		}

		return "User Defined and RFU\r\n";
	}


	private byte MakeLRC(byte[] data, Int32 length){
		byte lrc;
		
		if(length <= 0){
			return 0x00;
		}
		
		lrc = data[0];

		for(Int32 i=1;i<length;i++){
			lrc = (byte)(lrc ^ data[i]);
		}
		
		return lrc;
	}



	/*
	 * Remarks		: bMessageType will be included
	 */
	public static Int32 GetDataLength(byte[] arr){
		// Check array size ( Must bigger than 5 )
		if(arr.Length < 5){
			return -1;
		}

		Int32 nLength;
		
		nLength = (arr[4]*0x1000000) + (arr[3]*0x10000) + (arr[2]*0x100) + arr[1];
		
		return nLength;
	}



	/*
	 * Remarks		: bMessageType will be included
	 */
	public static void SetDataLength(byte[] arr, Int32 nLength){
		// Check array size ( Must bigger than 5 )
		if(arr.Length < 5){
			return;
		}

		// Split into byte array
		arr[4] = (byte)(nLength >> 24);
		arr[3] = (byte)((nLength >> 16) & 0xFF);
		arr[2] = (byte)((nLength >> 8) & 0xFF);
		arr[1] = (byte)(nLength & 0xFF);
	}
}
