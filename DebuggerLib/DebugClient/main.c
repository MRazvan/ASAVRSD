/*
 * FIRST.c
 *
 * Created: 2/23/2017 11:47:10 AM
 * Author : razvanm
 */ 

#include <stdint.h>
#include <avr/io.h>
#include <avr/interrupt.h>

#include <debug.h>

volatile uint16_t data = 0;
ISR (TIMER1_OVF_vect)    // Timer1 ISR
{
	TCNT1	  = 34286;   
	PORTB    &= !_BV(PORTB5);
	data++;
}

uint8_t eeprom_read(uint16_t addr){
	while(EECR & (1<<EEPE));
	/* Set up address register */
	EEAR = addr;
	/* Start eeprom read by writing EERE */
	EECR |= (1<<EERE);
	/* Return data from Data Register */
	return EEDR;
}

volatile uint16_t count = 0;
int main(void)
{
	DDRB = 0xFF;
	sei();
    /* Replace with your application code */
    while (1) 
    {
		count++;
		if (count % 2 == 0){
			// This is here so we can test the fact that the debug server can ignore
			//		UART traffic it does not care about
			DEBUG_BRK;
			dbg_write("Hello World   - %d\n", eeprom_read(0));
		}
		do{
			data++;
		}while(data % 91 != 0);
    }
}