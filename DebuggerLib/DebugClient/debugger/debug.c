/*
 * debug.c
 *
 * Created: 2/27/2017 10:38:47 AM
 *  Author: razvanm
 */ 

#include <stdint.h>
#include <avr/io.h>
#include <avr/common.h>
#include <avr/interrupt.h>
#include <avr/pgmspace.h>

#include "debug_defs.h"
#include "debug.h"

#define	AVR8_SWINT_PIN		(PORTD2)
#define AVR8_SWINT_INTMASK	(INT0)

//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
//	This assumes the following for now
//	- Clock - 16Mhz
//	- 16 Bit PC / STACK register sizes
//	- 16 Bit pointers
//	- At most 64k flash
//  ---------------
//	- Basically an ATmega328P
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////


//	MINIMIZE_RAM will force inline everything, so we don't have to push / pop registers between calls
//			it will however increase the flash size, we might even figure out a way to optimize the ram structure ????
//	MINIMIZE_FLASH will use the stack to push / pop registers when needed, if the stack is full, this will cause issues.
#define MINIMIZE_FLASH

//////////////////////////////////////////////////////////////////////////
// THE CAPABILITIES WE WANT TO HAVE IN THE DEBUGGER
//	By default we only have "Read ram" without saving the registers
//////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////
//	Enable the functionality to change the RAM memory
//CAPS_RAM_WRITE

//////////////////////////////////////////////////////////////////////////
//	Enable high speed UART (500000) this maps perfectly with 4,8,16,20Mhz crystals
//		it also allows us to read / write fast from / to memory
//CAPS_UART_HIGH_SPEED

//////////////////////////////////////////////////////////////////////////
//	Save the registers (R0...R31), the return PC address (from stack) and the stack pointer
//CAPS_SAVE_CTX

//////////////////////////////////////////////////////////////////////////
//	Enable flash read so we can read the program from memory
//CAPS_FLASH_READ

//////////////////////////////////////////////////////////////////////////
//	Enable flash write so we can change the program memory (AVR's support this only from bootloader)
//CAPS_FLASH_WRITE

//////////////////////////////////////////////////////////////////////////
//	Enable EEPROM read
//CAPS_EEPROM_READ

//////////////////////////////////////////////////////////////////////////
// Enable EEPROM write
//CAPS_EEPROM_WRITE

//////////////////////////////////////////////////////////////////////////
//	Enable code execution / method invocation, not done for now
//CAPS_EXECUTE

//////////////////////////////////////////////////////////////////////////
//	Enable single step support
//CAPS_SINGLE_STEP

//////////////////////////////////////////////////////////////////////////
//	Read the debug structure information in cases where we don't have access
//		to the compiled program (elf file). For example when the debugger is 
//		included in the bootloader, we don't have the bootloader file at our disposal
//		we need to know the location of the debug context, the structure will be based
//		on the CAPS enabled and retrieved when the debugging starts
//CAPS_DBG_CTX_ADDR


#if defined(MINIMIZE_FLASH) && defined(MINIMIZE_RAM)
#error	"Cannot minimize flash an ram at the same time, please choose only one"
#endif

#define INLINE	static inline
#if defined(MINIMIZE_RAM)
	#define ATTRIBUTES	__attribute__((always_inline, optimize("-Os")))
#elif defined(MINIMIZE_FLASH)
	#define ATTRIBUTES    __attribute__ ((optimize("-Os")))
#else
	#define ATTRIBUTES    __attribute__ ((optimize("-Os")))
#endif

#ifndef _BV
	#define _BV(v) (1 << (v))
#endif

#ifdef CAPS_SINGLE_STEP
	#define CAPS_FLAG_SINGLE_STEP _BV(CAPS_SINGLE_STEP_BIT)
	#ifndef CAPS_SAVE_CTX
		#define CAPS_SAVE_CTX
	#endif
#else
	#define CAPS_FLAG_SINGLE_STEP 0
#endif

#ifdef CAPS_UART_HIGH_SPEED
	//////////////////////////////////////////////////////////////////////////
	//	 BAUD PRESCALLER Used by the debug functionality, this will make 500k communication
	//		TODO Calculate this based on the CPU frequency, for now assume 16Mhz
	#define BAUD_PRESCALLER 1
	#define CAPS_FLAG_UART_HIGH_SPEED _BV(CAPS_UART_HIGHSPEED_BIT)
#else
	#define CAPS_FLAG_UART_HIGH_SPEED 0
#endif

#ifdef CAPS_SAVE_CTX
	#ifndef CAPS_DBG_CTX_ADDR
		#define CAPS_DBG_CTX_ADDR
	#endif
	#define CAPS_FLAG_SAVE_CTX _BV(CAPS_SAVE_CONTEXT_BIT)
#else
	#define CAPS_FLAG_SAVE_CTX 0
#endif

#ifdef CAPS_RAM_WRITE
	#define CAPS_FLAG_RAM_WRITE _BV(CAPS_RAM_W_BIT)
#else
	#define CAPS_FLAG_RAM_WRITE 0
#endif

#ifdef CAPS_FLASH_READ
	#define CAPS_FLAG_FLASH_READ	_BV(CAPS_FLASH_R_BIT)
#else
	#define CAPS_FLAG_FLASH_READ	0
#endif

#ifdef CAPS_FLASH_WRITE
	#define CAPS_FLAG_FLASH_WRITE _BV(CAPS_FLASH_W_BIT)
#else
	#define CAPS_FLAG_FLASH_WRITE	0
#endif

#ifdef CAPS_EEPROM_READ
	#define CAPS_FLAG_EEPROM_READ _BV(CAPS_EEPROM_R_BIT)
#else
	#define CAPS_FLAG_EEPROM_READ	0
#endif

#ifdef CAPS_EEPROM_WRITE
	#define CAPS_FLAG_EEPROM_WRITE _BV(CAPS_EEPROM_W_BIT)
#else
	#define CAPS_FLAG_EEPROM_WRITE	0
#endif

#ifdef CAPS_EXECUTE
	#define CAPS_FLAG_EXECUTE _BV(CAPS_EXECUTE_BIT)
#else
	#define CAPS_FLAG_EXECUTE 0
#endif

#ifdef CAPS_DBG_CTX_ADDR
	#define CAPS_FLAG_DBG_CTX_ADDR	_BV(CAPS_DBG_CTX_ADDR_BIT)
#else
	#define CAPS_FLAG_DBG_CTX_ADDR 0
#endif

//////////////////////////////////////////////////////////////////////////
//	The debug capabilities we have
#define CAPS_0	_BV(CAPS_RAM_R_BIT) | CAPS_FLAG_RAM_WRITE | CAPS_FLAG_FLASH_READ | CAPS_FLAG_FLASH_WRITE | CAPS_FLAG_EEPROM_READ | CAPS_FLAG_EEPROM_WRITE | CAPS_FLAG_EXECUTE | CAPS_FLAG_DBG_CTX_ADDR
#define CAPS_1	CAPS_FLAG_UART_HIGH_SPEED | CAPS_FLAG_SAVE_CTX | CAPS_FLAG_SINGLE_STEP

#define DBG_FLAG_EXECUTING			0
#define DBG_FLAG_UART_HIGH_SPEED	1
#define DBG_SINGLE_STEP_ISR			2
#define DBG_WILL_SINGLE_STEP		3

#define INTERRUPT_FLAG_BIT			7

//////////////////////////////////////////////////////////////////////////
//	Debug data sent when entering debug, so we know we are debugging and what capabilities we have
const uint8_t DEBUG_INFO[] PROGMEM = {DEBUG_COM_KEY_0, DEBUG_COM_KEY_1, DEBUG_COM_KEY_2, DEBUG_COM_KEY_3, DEBUG_COM_KEY_4, DEBUG_PROTOCOL_VERSION, SIGNATURE_0, SIGNATURE_1, SIGNATURE_2, CAPS_0, CAPS_1, DEBUG_COM_DATA_FILLER};

typedef struct {
	uint8_t l;
	uint8_t h;
} st_16bit_struct;

typedef union {
	st_16bit_struct d8;
	uint16_t d16;
} tu_uint16;

typedef union {
	st_16bit_struct nibbles;
	uint16_t addr;
	uint8_t* ptr;
} tu_puint8;

typedef struct {
	tu_uint16 size;
	tu_puint8 buff;
} t_mem_op_8;

#ifdef CAPS_UART_HIGH_SPEED
typedef struct {
	uint8_t ubrrh;
	uint8_t ubrrl;
	uint8_t ucsrc;
	uint8_t ucsrb;
} t_uart_regs;
#endif

#if defined(CAPS_EEPROM_READ) || defined(CAPS_EEPROM_WRITE)
typedef struct {
	uint16_t eear;
	#ifdef CAPS_EEPROM_WRITE
		uint8_t eedr;
	#endif 
} t_eeprom_regs;
#endif

#ifdef CAPS_SAVE_CTX
typedef struct {
	uint8_t _registers[32];
	tu_puint8 _stack_ptr;
	tu_uint16 _return_addr;
} t_state_ctx;
#endif

//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
//  DEBUG CONTEXT STRUCTURE, Define everything we need here
//		so we know at build time how much ram we need, also the compiler can optimize the memory access with a base pointer and offset
//	It is important that the registers are in the first part of the structure
//	We are doing asm with offset loads, and the largest offset is 64
//	So the registers must be in the first 64 bytes of the structure
typedef struct {
//////////////////////////////////////////////////////////////////////////
//	Save registers / PC / stack pointer
#ifdef CAPS_SAVE_CTX
	t_state_ctx registers;
#endif
//////////////////////////////////////////////////////////////////////////
//	High speed UART
#ifdef CAPS_UART_HIGH_SPEED
	t_uart_regs uart_regs;
#endif
//////////////////////////////////////////////////////////////////////////
//	EEprom functionality
#if defined(CAPS_EEPROM_READ) || defined(CAPS_EEPROM_WRITE)
	t_eeprom_regs eeprom_regs;
#endif
//////////////////////////////////////////////////////////////////////////
//	Execute support
#if defined(CAPS_EXECUTE) || defined(CAPS_UART_HIGH_SPEED) || defined(CAPS_SINGLE_STEP)
	uint8_t ctx_state;
#endif
	t_mem_op_8 mem_op;
	uint8_t tmp_u8;
	uint8_t status_reg;
	uint8_t watchdog;
} t_dbg_context;

//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////
// We don't need the context to be initialized, we don't care about the data
//	that is originally in there
t_dbg_context dbg_context;
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////

#ifdef CAPS_SINGLE_STEP
INLINE
ATTRIBUTES
void enable_single_step(){
	EICRA &= ~(_BV(ISC01) | _BV(ISC00));
	DDRD |= _BV(AVR8_SWINT_PIN);		/* set pin to output mode */
	EIFR |= _BV(AVR8_SWINT_INTMASK);	/* clear INTx flag */
	EIMSK |= _BV(AVR8_SWINT_INTMASK);	/* enable INTx interrupt */
	PORTD &= ~_BV(AVR8_SWINT_PIN);		/* make sure the pin is low */
}

INLINE
ATTRIBUTES
void disable_single_step(){
	EIMSK &= ~_BV(AVR8_SWINT_INTMASK);
}
#endif

//////////////////////////////////////////////////////////////////////////
#ifdef CAPS_UART_HIGH_SPEED

INLINE 
ATTRIBUTES
void dbg_init_uart(){
	UBRR0H = (uint8_t)(BAUD_PRESCALLER>>8);
	UBRR0L = (uint8_t)(BAUD_PRESCALLER);
	UCSR0B = (1<<RXEN0)|(1<<TXEN0);
	UCSR0C = ((1<<UCSZ00)|(1<<UCSZ01));	
}

INLINE
ATTRIBUTES
void dbg_save_uart_state(){
	dbg_context.uart_regs.ubrrh = UBRR0H;
	dbg_context.uart_regs.ubrrl = UBRR0L;
	dbg_context.uart_regs.ucsrb = UCSR0B;
	dbg_context.uart_regs.ucsrc = UCSR0C;
}

INLINE 
ATTRIBUTES
void dbg_restore_uart_state(){
	UBRR0H = dbg_context.uart_regs.ubrrh;
	UBRR0L = dbg_context.uart_regs.ubrrl;
	UCSR0B = dbg_context.uart_regs.ucsrb;
	UCSR0C = dbg_context.uart_regs.ucsrc;
}

#endif
//////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////
#ifdef CAPS_SAVE_CTX
//////////////////////////////////////////////////////////////////////////
//	 Save registers

//// Register save inspired from https://www.codeproject.com/articles/1037057/debugger-for-arduino
INLINE
ATTRIBUTES
void dbg_save_registers(){
asm volatile (	"sts	%[regs]+31, r31			\n\t" // R31
				"in		r31, __SREG__			\n\t"
				"cli							\n\t"
				"sts	%[statusaddr], r31		\n\t" // SREG
				"sts	%[regs]+30, r30			\n\t" // R30
				// Save registers from 0 to 27 inclusive
				"ldi	r30, lo8(%[regs])		\n\t" // load with regs ptr
				"ldi	r31, hi8(%[regs])		\n\t" // load with regs ptr Z
				"std	z+29, r29				\n\t"
				"std	z+28, r28				\n\t"
				"std	z+27, r27				\n\t"
				"ldi	r28, 0					\n\t"
				"ldi	r29, 0					\n\t"
			"1:	 ld		r27, y+					\n\t"
				"st		z+, r27					\n\t"
				"cpi	r28, 27					\n\t"
				"brne	1b						\n\t"
				// Save PC
				"pop	r15						\n\t" // Return ADDR HIGH
				"std	z+35-27, r15			\n\t"
				"pop	r16						\n\t" // Return ADDR LOW
				"std	z+34-27, r16			\n\t"
				"push	r16						\n\t" // Return ADDR LOW
				"push	r15						\n\t" // Return ADDR HIGH
				// Save stack
				"in		r15, __SP_L__			\n\t"
				"std	z+32-27, r15			\n\t"				
				"in		r15, __SP_H__			\n\t"
				"std	z+33-27, r15			\n\t"
				// And finally clear r1, the compiler expects this to be 0
				"clr	__zero_reg__			\n\t"
				::	[statusaddr] "i" (&dbg_context.status_reg),
					[regs] "i" (&dbg_context.registers)
		);
}

INLINE
ATTRIBUTES
void dbg_restore_registers(){
asm volatile (	"ldi	r30, lo8(%[regs])		\n\t" // load with regs ptr
				"ldi	r31, hi8(%[regs])		\n\t" // load with regs ptr Z
				// Restore the registers from 0 to 27 (inclusive)
				"ldi	r28, 0					\n\t"
				"ldi	r29, 0					\n\t"
			"1:	 ld		r27, z+					\n\t"
				"st		y+, r27					\n\t"
				"cpi	r28, 27					\n\t"
				"brne	1b						\n\t"
				// Restore registers from 28 to 31 and the SREG register
				"ld		r27, z					\n\t"
				"ldd	r28, z+28-27			\n\t"
				"ldd	r29, z+29-27			\n\t"
				"ldd	r30, z+30-27			\n\t"
				"lds	r31, %[statusaddr]		\n\t"
				"out	__SREG__, r31			\n\t"
				"lds	r31, %[regs]+31			\n\t"
				::	[statusaddr] "i" (&dbg_context.status_reg),
					[regs] "i" (&dbg_context.registers)
				);
}
#endif
//////////////////////////////////////////////////////////////////////////

#if defined(CAPS_EEPROM_READ) || defined(CAPS_EEPROM_WRITE)

#ifdef CAPS_EEPROM_READ
INLINE
ATTRIBUTES
uint8_t eeprom_read(uint16_t addr){
	/* Wait for completion of previous write */
	while(EECR & (1<<EEPE));
	/* Set up address register */
	EEAR = addr;
	/* Start eeprom read by writing EERE */
	EECR |= (1<<EERE);
	/* Return data from Data Register */
	return EEDR;
}
#endif

#ifdef CAPS_EEPROM_WRITE
INLINE
ATTRIBUTES
void eeprom_write(uint16_t addr, uint8_t data){
	/* Wait for completion of previous write */
	while(EECR & (1<<EEPE));
	/* Set up address and Data Registers */
	EEAR = addr;
	EEDR = data;
	/* Write logical one to EEMPE */
	EECR |= (1<<EEMPE);
	/* Start eeprom write by setting EEPE */
	EECR |= (1<<EEPE);
}
#endif

INLINE
ATTRIBUTES
void dbg_save_eeprom(){
	dbg_context.eeprom_regs.eear = EEAR;
	#ifdef CAPS_EEPROM_WRITE
		dbg_context.eeprom_regs.eedr = EEDR;
	#endif
}

INLINE
ATTRIBUTES
void dbg_restore_eeprom(){
	EEAR = dbg_context.eeprom_regs.eear;
	#ifdef CAPS_EEPROM_WRITE
		EEDR = dbg_context.eeprom_regs.eedr;
	#endif
}
#endif

INLINE
ATTRIBUTES
void dbg_enter_debug(){
#ifndef CAPS_SAVE_CTX
	// Disable interrupts, we don't need / want any
	dbg_context.status_reg = SREG;
	cli();
#else
	// Disable the interrupts
	// Save the registers (we are doing a `context switch`)
	dbg_save_registers();
#endif
}

INLINE
ATTRIBUTES
void dbg_leave_debug(){
#ifdef CAPS_SAVE_CTX
	//	Context switch again
	dbg_restore_registers();
#else
	// Restore the status registers, this will enable interrupts if they were 
	//	enabled when we entered debug
	SREG = dbg_context.status_reg;
#endif
}

INLINE
ATTRIBUTES
void dbg_disable_watchdog(){
	dbg_context.watchdog = WDTCSR;
	// Disable watchdog
	WDTCSR ^= ~_BV(WDE);
}

INLINE
ATTRIBUTES
void dbg_restore_watchdog(){
	WDTCSR = dbg_context.watchdog;
}

uint8_t dbg_get_ch(){
	while(!(UCSR0A & (1<<RXC0)));
	return UDR0;
}

void dbg_put_ch(uint8_t data){
	while(!(UCSR0A & (1<<UDRE0)));
	UDR0 = data;
}

INLINE
ATTRIBUTES
void dbg_read_mem_op(){
	// 2 bytes - the address
	dbg_context.mem_op.buff.nibbles.h = dbg_get_ch();
	dbg_context.mem_op.buff.nibbles.l = dbg_get_ch();
	// Next 2 bytes the size to read
	dbg_context.mem_op.size.d8.h = dbg_get_ch();
	dbg_context.mem_op.size.d8.l = dbg_get_ch();	
}

INLINE
ATTRIBUTES
void dbg_send_info(){
	for(dbg_context.tmp_u8 = 0; dbg_context.tmp_u8 < sizeof(DEBUG_INFO); ++dbg_context.tmp_u8){
		dbg_put_ch(pgm_read_byte_near(DEBUG_INFO + dbg_context.tmp_u8));
	}
}

INLINE
ATTRIBUTES
void dbg_disable_interrupts(){
	dbg_context.status_reg &= ~_BV(INTERRUPT_FLAG_BIT);
}

#ifdef CAPS_SAVE_CTX
__attribute__ ((naked, optimize("-Os")))
#else
__attribute__ ((optimize("-Os")))
#endif
void dbg_brk() {

	dbg_enter_debug();

#ifdef CAPS_EXECUTE
	if (dbg_context.ctx_state & _BV(DBG_FLAG_EXECUTING)){
		dbg_leave_debug();
		asm volatile("ret \n\t");
	}
	else dbg_context.ctx_state |= _BV(DBG_FLAG_EXECUTING);
#endif

#ifdef CAPS_SINGLE_STEP
if (dbg_context.ctx_state & _BV(DBG_SINGLE_STEP_ISR)){
	dbg_context.ctx_state &= ~_BV(DBG_SINGLE_STEP_ISR);
	disable_single_step();
}
#endif

//////////////////////////////////////////////////////////////////////////
// Disable the Watchdog
	dbg_disable_watchdog();

//////////////////////////////////////////////////////////////////////////
// SAVE EEPROM REGISTERS
#if defined(CAPS_EEPROM_WRITE) | defined(CAPS_EEPROM_READ)
	dbg_save_eeprom();
#endif

//////////////////////////////////////////////////////////////////////////
// Notify we reached a debug point
	dbg_send_info();

	while(1){
		dbg_context.tmp_u8 = dbg_get_ch();
		if (dbg_context.tmp_u8 == DEBUG_REQ_CONTINUE){
			break;
		} else if (dbg_context.tmp_u8 == DEBUG_REQ_READ_RAM){
			dbg_read_mem_op();
			while(dbg_context.mem_op.size.d16--){
				dbg_put_ch(*dbg_context.mem_op.buff.ptr++);
			}
		}
	// Everything bellow is optional depending on how big we want the debug code to be
	//		and what functionality we want to / can have
	#ifdef CAPS_RAM_WRITE
	else if (dbg_context.tmp_u8 == DEBUG_REQ_WRITE_RAM){
		// Normally we should check to see where we are writing to
		//		If we are writing to any SAVED registers
		//			we want to update the saved data and not the live one
		//			however this can be more easily handled in the debug server than on the client
		//			this way the code on the client is small, fast and clean.
		dbg_read_mem_op();
		while(dbg_context.mem_op.size.d16--){
			*dbg_context.mem_op.buff.ptr++ = dbg_get_ch();
		}
	}
	#endif
	#ifdef CAPS_UART_HIGH_SPEED
		else if (dbg_context.tmp_u8 == DEBUG_REQ_UART_HIGH_SPEED){
			// Save the UART configuration
			dbg_save_uart_state();

			// Initialize debug communication
			dbg_init_uart();
			dbg_context.ctx_state |= _BV(DBG_FLAG_UART_HIGH_SPEED);
		}
	#endif
	#ifdef CAPS_DBG_CTX_ADDR
		// This is needed in case we don't have the elf file,
		//		and we don't know where the context is located in memory
		//		and what size it has
		else if (dbg_context.tmp_u8 == DEBUG_REQ_GET_CTX_ADDR){
			dbg_put_ch(((uint16_t)&dbg_context) >> 8);
			dbg_put_ch(((uint16_t)&dbg_context) & 0xFF);
			uint16_t size = sizeof(dbg_context);
			dbg_put_ch(size >> 8);
			dbg_put_ch(size & 0xFF);
		}
	#endif
	#ifdef CAPS_FLASH_READ
		else if (dbg_context.tmp_u8 == DEBUG_REQ_READ_FLASH){
			dbg_read_mem_op();
			while(dbg_context.mem_op.size.d16--){
				dbg_put_ch(pgm_read_byte(dbg_context.mem_op.buff.addr++));
			}
		} 
	#endif
	#ifdef CAPS_FLASH_WRITE
		else if (dbg_context.tmp_u8 == DEBUG_REQ_WRITE_FLASH){
			// nothing for now, AVR's only supports write to flash from bootloader
			//	since we are not part of it, we can't write to flash
			break;
		} 
	#endif
	#ifdef CAPS_EEPROM_READ
		else if (dbg_context.tmp_u8 == DEBUG_REQ_READ_EEPROM){
			dbg_read_mem_op();
			while(dbg_context.mem_op.size.d16--){
				dbg_put_ch(eeprom_read(dbg_context.mem_op.buff.addr++));
			}
		} 
	#endif
	#ifdef CAPS_EEPROM_WRITE
		else if (dbg_context.tmp_u8 == DEBUG_REQ_WRITE_EEPROM){
			dbg_read_mem_op();
			while(dbg_context.mem_op.size.d16--){
				eeprom_write(dbg_context.mem_op.buff.addr++, dbg_get_ch());
			}
		}
	#endif
	#ifdef CAPS_SINGLE_STEP
		else if (dbg_context.tmp_u8 == DEBUG_REQ_SINGLE_STEP){
			enable_single_step();
			dbg_context.ctx_state |= _BV(DBG_WILL_SINGLE_STEP);
			// Exit the read loop and return to caller
			break;
		}
	#endif
	#ifdef CAPS_EXECUTE
		else if (dbg_context.tmp_u8 == DEBUG_REQ_EXECUTE){
			// Nothing for now, we should call methods from here
			//	TODO: Figure out the how we receive / setup parameters, 
			//	and how we are going to send data back after we do the call
			//	Also how do we handle strings sent as parameters, we need to setup a memory region and copy the string there?
			break;
		}
	#endif
	}

#ifdef CAPS_UART_HIGH_SPEED
	//	RESTORE the UART registers
	if (dbg_context.ctx_state & _BV(DBG_FLAG_UART_HIGH_SPEED)){
		dbg_restore_uart_state();
		dbg_context.ctx_state &= ~_BV(DBG_FLAG_UART_HIGH_SPEED);
	}
#endif

//////////////////////////////////////////////////////////////////////////
// RESTORE EEPROM REGISTERS
#if defined(CAPS_EEPROM_WRITE) | defined(CAPS_EEPROM_READ)
	dbg_restore_eeprom();
#endif

#ifdef CAPS_EXECUTE
	// Clear execute flag
	dbg_context.ctx_state &= ~_BV(DBG_FLAG_EXECUTING);
#endif

	// Restore the watchdog register
	dbg_restore_watchdog();

#ifdef CAPS_SINGLE_STEP
	if (dbg_context.ctx_state & _BV(DBG_SINGLE_STEP_ISR)){
		if (dbg_context.ctx_state & _BV(DBG_WILL_SINGLE_STEP)){
			// Do not clear the ISR state just return from the current ISR
			dbg_context.ctx_state &= ~_BV(DBG_WILL_SINGLE_STEP);
		}else{
			// We will not single step again
			//		clear the flag and return from ISR
			dbg_context.ctx_state &= ~_BV(DBG_SINGLE_STEP_ISR);
		}
		dbg_disable_interrupts();
		dbg_leave_debug();
		asm volatile ("reti \n\t");
	}else if (dbg_context.ctx_state & _BV(DBG_WILL_SINGLE_STEP)){
		dbg_context.ctx_state &= ~_BV(DBG_WILL_SINGLE_STEP);
		dbg_context.ctx_state |= _BV(DBG_SINGLE_STEP_ISR);
		dbg_disable_interrupts();
		dbg_leave_debug();
		asm volatile ("reti \n\t");
	}
#endif
	// Leave debug and set back the status register
	//		this will re-enable interrupts if needed
	dbg_leave_debug();
	asm volatile ("ret \n\t");
}

#ifdef CAPS_SINGLE_STEP
	ISR ( INT0_vect, ISR_BLOCK ISR_NAKED ){
		asm volatile ("jmp dbg_brk");
	}
#endif

//__attribute__((used, naked))
//void __vector_default(){
	//dbg_brk();
//}