#line 1 "C:/Users/utku/Documents/GitHub/DCMotorSpeed-DirectionControl/MikroC/MotorSpeed&DirectionControl.c"

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





const unsigned short TEMP_RESOLUTION = 9;

char *text = "000.0000";
char *whole = "000.0000";
char *fr = "000.0000";
char *strPwm = "000.0000";

char uart_rd;

unsigned temp;
int pwm =0;
int counter = 0;
#line 44 "C:/Users/utku/Documents/GitHub/DCMotorSpeed-DirectionControl/MikroC/MotorSpeed&DirectionControl.c"
void Display_Temperature(unsigned int temp2write) {

 const unsigned short RES_SHIFT = TEMP_RESOLUTION - 8;
 char temp_whole;
 unsigned int temp_fraction;


 if (temp2write & 0x8000) {
 text[0] = '-';
 temp2write = ~temp2write + 1;
 }


 temp_whole = temp2write >> 4 ;


 if (temp_whole/100)
 text[0] = temp_whole/100 + 48;
 else
 text[0] = '0';

 text[1] = (temp_whole/10)%10 + 48;
 text[2] = temp_whole%10 + 48;


 temp_fraction = temp2write;
 temp_fraction &= 0x000F;
 temp_fraction *= 625;


 text[4] = temp_fraction/1000 + 48;
 text[5] = (temp_fraction/100)%10 + 48;
 text[6] = (temp_fraction/10)%10 + 48;
 text[7] = temp_fraction%10 + 48;


 Lcd_Out(2, 5, text);


 if(PORTD.F2 == 1){
 IntToStr(temp_whole,whole);
 IntToStr(temp_fraction,fr);
 IntToStr(pwm,strPwm);
 UART1_Write_Text("{\"Temprature\":\"");
 UART1_Write_Text(text);
 UART1_Write_Text("\",\"PWMDuty\":\"");
 UART1_Write_Text(strPwm);
 UART1_Write_Text("\"}");
 UART1_Write(10);
 UART1_Write(13);
 }
}
void Interrupt(){
if(PIR1.RCIF){


 uart_rd = UART1_Read();


 PORTB.F3 = 1;

 PORTB = uart_rd & 0x80;

 PORTB.F6 = ~PORTB.F7 ;

 uart_rd = uart_rd & 0x7F;

 uart_rd = uart_rd << 1;

 pwm = uart_rd;
 PORTB.F3 = 0;
 }
}
void main() {
 ANSEL = 0;
 ANSELH = 0;
 C1ON_bit = 0;
 C2ON_bit = 0;
 TRISC = 0;
 PORTC = 0;
 TRISD = 0xFF;
 PORTD = 0;
 TRISB = 0;
 PORTB = 255;
 Delay_ms(500);
 PORTB = 0;

 INTCON.GIE = 1;
 INTCON.PEIE = 1;
 PIE1.RCIE=1;

 UART1_Init(9600);
 Delay_ms(100);

 PWM1_Init(5000);
 PWM1_Start();
 PWM1_Set_Duty(0);

 Lcd_Init();
 Lcd_Cmd(_LCD_CLEAR);
 Lcd_Cmd(_LCD_CURSOR_OFF);
 Lcd_Out(1, 1, " Temperature:   ");

 Lcd_Chr(2,13,223);


 Lcd_Chr(2,14,'C');


 do {

 Ow_Reset(&PORTE, 2);
 Ow_Write(&PORTE, 2, 0xCC);
 Ow_Write(&PORTE, 2, 0x44);
 Delay_us(120);

 Ow_Reset(&PORTE, 2);
 Ow_Write(&PORTE, 2, 0xCC);
 Ow_Write(&PORTE, 2, 0xBE);

 temp = Ow_Read(&PORTE, 2);
 temp = (Ow_Read(&PORTE, 2) << 8) + temp;

 if(PORTD.F2 == 1){
 if(counter > 10){

 Display_Temperature(temp);
 counter = 0;
 }
 }
 counter = counter + 1;
 PWM1_Set_Duty(pwm);

 asm CLRWDT;
 } while (1);
}
