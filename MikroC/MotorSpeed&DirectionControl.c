// LCD module connections
sbit LCD_RS at RB4_bit;
sbit LCD_EN at RB5_bit;
sbit LCD_D4 at RB0_bit;
sbit LCD_D5 at RB1_bit;
sbit LCD_D6 at RB2_bit;
sbit LCD_D7 at RB3_bit;

sbit LCD_RS_Direction at TRISB4_bit;
sbit LCD_EN_Direction at TRISB5_bit;
sbit LCD_D4_Direction at TRISB0_bit;
sbit LCD_D5_Direction at TRISB1_bit;
sbit LCD_D6_Direction at TRISB2_bit;
sbit LCD_D7_Direction at TRISB3_bit;
// End LCD module connections

//  Set TEMP_RESOLUTION to the corresponding resolution of used DS18x20 sensor:
//  18S20: 9  (default setting; can be 9,10,11,or 12)
//  18B20: 12
const unsigned short TEMP_RESOLUTION = 9;

char *text = "000.0000";
char *whole = "000.0000";
char *fr = "000.0000";
char *strPwm = "000.0000";

char uart_rd;

unsigned temp;
int pwm;
// Mapping function to convert Temprature value to PWM duty.
  int Map(int Value, int FromLow, int FromHigh, int ToLow, int ToHigh ){
if(Value<FromLow){
Value = FromLow;
}
if(Value>FromHigh){
Value = FromHigh;
}
return  (int)(((Value - FromLow) * (ToHigh - ToLow)) / (FromHigh - FromLow)) + ToLow;
}

void Display_Temperature(unsigned int temp2write) {
  const unsigned short RES_SHIFT = TEMP_RESOLUTION - 8;
  char temp_whole;
  unsigned int temp_fraction;

  // Check if temperature is negative
  if (temp2write & 0x8000) {
     text[0] = '-';
     temp2write = ~temp2write + 1;
     }

  // Extract temp_whole
  temp_whole = temp2write >> 4 ;

  // Convert temp_whole to characters
  if (temp_whole/100)
     text[0] = temp_whole/100  + 48;
  else
     text[0] = '0';

  text[1] = (temp_whole/10)%10 + 48;             // Extract tens digit
  text[2] =  temp_whole%10     + 48;             // Extract ones digit

  // Extract temp_fraction and convert it to unsigned int
  temp_fraction  = temp2write;
  temp_fraction &= 0x000F;
  temp_fraction *= 625;

  // Convert temp_fraction to characters
  text[4] =  temp_fraction/1000    + 48;         // Extract thousands digit
  text[5] = (temp_fraction/100)%10 + 48;         // Extract hundreds digit
  text[6] = (temp_fraction/10)%10  + 48;         // Extract tens digit
  text[7] =  temp_fraction%10      + 48;         // Extract ones digit

  // Print temperature on LCD
  Lcd_Out(2, 5, text);

  //Send datas via bluetooth or Uart as json format string
  if(PORTD.F2 == 1){
      IntToStr(temp_whole,whole);
      IntToStr(temp_fraction,fr);
      IntToStr(pwm,strPwm);
      UART1_Write_Text("{\"Temprature\":\"");
      UART1_Write_Text(whole);
      UART1_Write_Text(".");
      UART1_Write_Text(fr);
      UART1_Write_Text("\",\"PWMDuty\":\"");
      UART1_Write_Text(strPwm);
      UART1_Write_Text("\"}");
      UART1_Write(10);
      UART1_Write(13);

      }
}

void main() {
  ANSEL  = 0;                                    // Configure AN pins as digital I/O
  ANSELH = 0;
  C1ON_bit = 0;                                  // Disable comparators
  C2ON_bit = 0;
  TRISC = 0;
  PORTC = 0;
  TRISD = 0xFF;
  PORTD = 0;
  TRISB = 0;
  PORTB = 0;
  
  UART1_Init(9600);
  Delay_ms(100);

  PWM1_Init(5000);
  PWM1_Start();
  PWM1_Set_Duty(0);

  Lcd_Init();                                    // Initialize LCD
  Lcd_Cmd(_LCD_CLEAR);                           // Clear LCD
  Lcd_Cmd(_LCD_CURSOR_OFF);                      // Turn cursor off
  Lcd_Out(1, 1, " Temperature:   ");
  // Print degree character, 'C' for Centigrades
  Lcd_Chr(2,13,223);                             // Different LCD displays have different char code for degree
                                                 // If you see greek alpha letter try typing 178 instead of 223

  Lcd_Chr(2,14,'C');

  //--- Main loop
  do {
    //--- Perform temperature reading
    Ow_Reset(&PORTE, 2);                         // Onewire reset signal
    Ow_Write(&PORTE, 2, 0xCC);                   // Issue command SKIP_ROM
    Ow_Write(&PORTE, 2, 0x44);                   // Issue command CONVERT_T
    Delay_us(120);

    Ow_Reset(&PORTE, 2);
    Ow_Write(&PORTE, 2, 0xCC);                   // Issue command SKIP_ROM
    Ow_Write(&PORTE, 2, 0xBE);                   // Issue command READ_SCRATCHPAD

    temp =  Ow_Read(&PORTE, 2);
    temp = (Ow_Read(&PORTE, 2) << 8) + temp;

  if (UART1_Data_Ready()) {     // If data is received,
         //Read one byte data
          uart_rd = UART1_Read();
          //Write this data to Uart for monitoring
          UART1_Write(uart_rd);
          // Equal 7. bit of PORTB to 7. bit of data
          PORTB =  uart_rd & 0x80;
          // Equal 6. bit of PORTB to reverse of  7. bit of PORTB
          PORTB.F6 = ~PORTB.F7    ;
          // 7. bit to zero
          uart_rd = uart_rd & 0x7F;
          // data * 2 to calculate pwm duty
          uart_rd =   uart_rd << 1;
         PWM1_Set_Duty(uart_rd);

    }
  else{
         //--- Format and display result on Lcd
    //Display_Temperature(temp);
    }

  } while (1);
}