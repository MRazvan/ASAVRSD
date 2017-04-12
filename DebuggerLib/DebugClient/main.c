/*
 * FIRST.c
 *
 * Created: 2/23/2017 11:47:10 AM
 * Author : razvanm
 */ 

#include <stdint.h>
#include <avr/io.h>
#include <avr/interrupt.h>

#include "Debugger/debug.h"

#define BAUD_PRESCALLER 1
volatile uint16_t data = 0;
ISR (TIMER1_OVF_vect)    // Timer1 ISR
{
	TCNT1	  = 34286;   
	PORTB    &= !_BV(PORTB5);
	data++;
}

void _init_uart(){
	UBRR0H = (uint8_t)(BAUD_PRESCALLER>>8);
	UBRR0L = (uint8_t)(BAUD_PRESCALLER);
	UCSR0B = (1<<RXEN0)|(1<<TXEN0);
	UCSR0C = ((1<<UCSZ00)|(1<<UCSZ01));
}

void _uart_put_ch(uint8_t data){
	while(!(UCSR0A & (1<<UDRE0)));
	UDR0 = data;
	PINB = 0x20;
}

void _uart_write_str(char* str){
	while(*str != 0x00){
		_uart_put_ch(*str++);
	}
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
	_init_uart();
    /* Replace with your application code */
    while (1) 
    {
		count++;
		if (count % 2 == 0){
			// This is here so we can test the fact that the debug server can ignore
			//		UART traffic it does not care about
			DEBUG_BRK;
			_uart_write_str("Hello World   ");
			_uart_put_ch(eeprom_read(0));
			_uart_put_ch('\n');
		}
		do{
			data++;
		}while(data % 91 != 0);
    }
}