STM32U575 Firmware

Basic firmware project for the NUCLEO-U575ZI-Q development board.

Firmware Information

MCU: STM32U575ZITxQ

Board: NUCLEO-U575ZI-Q

Firmware Version: 0.0.1

Language: C

IDE: STM32CubeIDE

Features

UART communication over COM1 at 115200 baud

Interrupt-based UART receive

On-board LED control

User button status

I2C2 communication

BME280 environmental sensor support

Temperature, humidity, pressure and chip ID reading

UART Commands

TEST
LED 1
LED 0
GET LED
GET BUTTON
GET INFO
GET BME280


The BME280 is connected through I2C2 and uses I2C address 0x77.

This project uses the MIT-licensed BME280 STM32 driver:

https://github.com/Afebia/BME280-STM32-V2


Version

Current firmware version: 0.0.1