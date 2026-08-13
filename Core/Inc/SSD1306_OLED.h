/*
 * SSD1306_OLED.h
 *
 *  Created on: Aug 13, 2026
 *      Author: KemalT
 */

#ifndef INC_SSD1306_OLED_H_
#define INC_SSD1306_OLED_H_

#include "main.h"
#include <stdint.h>

#define SSD1306_WIDTH       128U
#define SSD1306_HEIGHT      64U
#define SSD1306_I2C_ADDR    (0x3C << 1)

HAL_StatusTypeDef SSD1306_Init(I2C_HandleTypeDef *hi2c);
HAL_StatusTypeDef SSD1306_UpdateScreen(void);

void SSD1306_Clear(void);
void SSD1306_DrawPixel(uint8_t x, uint8_t y, uint8_t on);
void SSD1306_SetCursor(uint8_t x, uint8_t y);
void SSD1306_WriteChar(char c);
void SSD1306_WriteString(const char *str);




#endif /* INC_SSD1306_OLED_H_ */
