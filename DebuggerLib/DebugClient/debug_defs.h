/*
 * debug_defs.h
 *
 * Created: 2/27/2017 10:37:37 AM
 *  Author: razvanm
 */ 


#ifndef DEBUG_DEFS_H_
#define DEBUG_DEFS_H_


#define DEBUG_COM_DATA_FILLER		0xFF

#define DEBUG_COM_KEY_0				0x21
#define DEBUG_COM_KEY_1				0xFE
#define DEBUG_COM_KEY_2				0xA9
#define DEBUG_COM_KEY_3				0x15
#define DEBUG_COM_KEY_4				0x84
//////////////////////////////////////////////////////////////////////////
//	Requests
#define DEBUG_REQ_CONTINUE			0x02
#define DEBUG_REQ_GET_CTX_ADDR		0x03
#define DEBUG_REQ_UART_HIGH_SPEED	0x05

#define DEBUG_REQ_READ_RAM			0x10
#define DEBUG_REQ_WRITE_RAM			0x11

#define DEBUG_REQ_READ_FLASH		0x12
#define DEBUG_REQ_WRITE_FLASH		0x13

#define DEBUG_REQ_READ_EEPROM		0x14
#define DEBUG_REQ_WRITE_EEPROM		0x15

#define DEBUG_REQ_SINGLE_STEP		0x16
#define DEBUG_REQ_SET_PC			0x17

// Not implemented for now
#define DEBUG_REQ_EXECUTE			0x20



//	8 bits
//	RAMR - 0 :	- 0 cannot read ram
//				- 1 can read ram
//	RAMW - 1 :	- 0 cannot write ram
//				- 1 can write ram
//	FLASHW-2 :	- 0 cannot write flash
//				- 1 can write flash
//	FLASHR-3 :	- 0 cannot read flash
//				- 1 can read flash
//	EEPROMW-4 :	- 0 cannot write eeprom
//				- 1 can write eeprom
//	EEPROMR-5 :	- 0 cannot read eeprom
//				- 1 can read eeprom
	#define CAPS_RAM_R_BIT				0
	#define CAPS_RAM_W_BIT				1
	#define CAPS_FLASH_R_BIT			2
	#define CAPS_FLASH_W_BIT			3
	#define CAPS_EEPROM_R_BIT			4
	#define CAPS_EEPROM_W_BIT			5
	#define CAPS_EXECUTE_BIT			6
	#define CAPS_DBG_CTX_ADDR_BIT		7

	#define CAPS_UART_HIGHSPEED_BIT		0
	#define CAPS_SAVE_CONTEXT_BIT		1
	#define CAPS_SINGLE_STEP_BIT		2
	#define CAPS_DISABLE_TIMERS_BIT		3

#endif /* DEBUG_DEFS_H_ */