/*
 * debug.h
 *
 * Created: 2/27/2017 10:38:31 AM
 *  Author: razvanm
 */ 


#ifndef DEBUG_H_
#define DEBUG_H_

#define DEBUG_PROTOCOL_VERSION		0x01

#ifdef __cplusplus
extern "C" {
#endif
// This should be used to call the debug function
//	It will force the compiler to place the PC on stack and not optimize the call
//	otherwise we could be in a situation where we are breaking in method_a, yet the compiler
//	has optimized the stack to return directly to the calling method

//	Normal stack
//	PC - Parent
//	PC - method_a
//	Debug method 

//	Optimized stack - we can't tell we were called from method_a
//	PC - Parent - method_a - optimized away
//	Debug method - return directly to parent
#ifdef DEBUG
	#define DEBUG_BRK	asm volatile ("call dbg_brk");
#else
	#define DEBUG_BRK
#endif

#ifdef __cplusplus
}
#endif

#endif /* DEBUG_H_ */