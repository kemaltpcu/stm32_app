/*
 * SSD1306_OLED.c
 *
 *  Created on: Aug 13, 2026
 *      Author: KemalT
 */

#include "SSD1306_OLED.h"
#include <string.h>

static I2C_HandleTypeDef *ssd1306_i2c = NULL;
static uint8_t ssd1306_buffer[SSD1306_WIDTH * SSD1306_HEIGHT / 8U];  //128 * 64 = 8192 pixels / 8 = 1024 bytes (1KB)

static uint8_t cursorX = 0U;
static uint8_t cursorY = 0U;

// 5x7 font: digits 0-9
static const uint8_t fontDigits[10][5] =
{
    {0x3E,0x51,0x49,0x45,0x3E}, /* 0 */
    {0x00,0x42,0x7F,0x40,0x00}, /* 1 */
    {0x42,0x61,0x51,0x49,0x46}, /* 2 */
    {0x21,0x41,0x45,0x4B,0x31}, /* 3 */
    {0x18,0x14,0x12,0x7F,0x10}, /* 4 */
    {0x27,0x45,0x45,0x45,0x39}, /* 5 */
    {0x3C,0x4A,0x49,0x49,0x30}, /* 6 */
    {0x01,0x71,0x09,0x05,0x03}, /* 7 */
    {0x36,0x49,0x49,0x49,0x36}, /* 8 */
    {0x06,0x49,0x49,0x29,0x1E}  /* 9 */
};

// 5x7 font: uppercase A-Z
static const uint8_t fontLetters[26][5] =
{
    {0x7E,0x11,0x11,0x11,0x7E}, /* A */
    {0x7F,0x49,0x49,0x49,0x36}, /* B */
    {0x3E,0x41,0x41,0x41,0x22}, /* C */
    {0x7F,0x41,0x41,0x22,0x1C}, /* D */
    {0x7F,0x49,0x49,0x49,0x41}, /* E */
    {0x7F,0x09,0x09,0x09,0x01}, /* F */
    {0x3E,0x41,0x49,0x49,0x7A}, /* G */
    {0x7F,0x08,0x08,0x08,0x7F}, /* H */
    {0x00,0x41,0x7F,0x41,0x00}, /* I */
    {0x20,0x40,0x41,0x3F,0x01}, /* J */
    {0x7F,0x08,0x14,0x22,0x41}, /* K */
    {0x7F,0x40,0x40,0x40,0x40}, /* L */
    {0x7F,0x02,0x0C,0x02,0x7F}, /* M */
    {0x7F,0x04,0x08,0x10,0x7F}, /* N */
    {0x3E,0x41,0x41,0x41,0x3E}, /* O */
    {0x7F,0x09,0x09,0x09,0x06}, /* P */
    {0x3E,0x41,0x51,0x21,0x5E}, /* Q */
    {0x7F,0x09,0x19,0x29,0x46}, /* R */
    {0x46,0x49,0x49,0x49,0x31}, /* S */
    {0x01,0x01,0x7F,0x01,0x01}, /* T */
    {0x3F,0x40,0x40,0x40,0x3F}, /* U */
    {0x1F,0x20,0x40,0x20,0x1F}, /* V */
    {0x3F,0x40,0x38,0x40,0x3F}, /* W */
    {0x63,0x14,0x08,0x14,0x63}, /* X */
    {0x07,0x08,0x70,0x08,0x07}, /* Y */
    {0x61,0x51,0x49,0x45,0x43}  /* Z */
};

static HAL_StatusTypeDef SSD1306_WriteCommand(uint8_t command)
{
    uint8_t packet[2];

    packet[0] = 0x00; // command control byte
    packet[1] = command;

    return HAL_I2C_Master_Transmit(ssd1306_i2c, SSD1306_I2C_ADDR, packet, 2, 100);
}

static void SSD1306_GetGlyph(char character, uint8_t glyph[5])
{
    memset(glyph, 0, 5U);

    if ((character >= 'a') && (character <= 'z'))
    {
    	character = (char)(character - ('a' - 'A'));
    }

    if ((character >= '0') && (character <= '9'))
    {
        memcpy(glyph, fontDigits[character - '0'], 5);
        return;
    }

    if ((character >= 'A') && (character <= 'Z'))
    {
        memcpy(glyph, fontLetters[character - 'A'], 5);
        return;
    }

    switch (character)
    {
        case ' ':
            break;

        case '.':
            glyph[2] = 0x60U;
            glyph[3] = 0x60U;
            break;

        case ':':
            glyph[2] = 0x14U;
            break;

        case '-':
            glyph[0] = 0x08U;
            glyph[1] = 0x08U;
            glyph[2] = 0x08U;
            glyph[3] = 0x08U;
            glyph[4] = 0x08U;
            break;

        case '%':
            glyph[0] = 0x63U;
            glyph[1] = 0x13U;
            glyph[2] = 0x08U;
            glyph[3] = 0x64U;
            glyph[4] = 0x63U;
            break;

        default:
            break;
    }
}

HAL_StatusTypeDef SSD1306_Init(I2C_HandleTypeDef *hi2c)
{
    if (hi2c == NULL)
    {
        return HAL_ERROR;
    }

    ssd1306_i2c = hi2c;

    HAL_Delay(100U);

    if (SSD1306_WriteCommand(0xAE) != HAL_OK) return HAL_ERROR; // display off
    if (SSD1306_WriteCommand(0xD5) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x80) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xA8) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x3F) != HAL_OK) return HAL_ERROR; // 64 rows
    if (SSD1306_WriteCommand(0xD3) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x00) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x40) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x8D) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x14) != HAL_OK) return HAL_ERROR; // charge pump
    if (SSD1306_WriteCommand(0x20) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x02) != HAL_OK) return HAL_ERROR; // page addressing
    if (SSD1306_WriteCommand(0xA1) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xC8) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xDA) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x12) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x81) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xCF) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xD9) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xF1) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xDB) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0x40) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xA4) != HAL_OK) return HAL_ERROR;
    if (SSD1306_WriteCommand(0xA6) != HAL_OK) return HAL_ERROR; // normal display
    if (SSD1306_WriteCommand(0xAF) != HAL_OK) return HAL_ERROR; // display on

    SSD1306_Clear();

    printf("Screen initialized successfully \r\n");
    return SSD1306_UpdateScreen();
}

void SSD1306_Clear(void)
{
    memset(ssd1306_buffer, 0x00, sizeof(ssd1306_buffer));
    cursorX = 0U;
    cursorY = 0U;
}

void SSD1306_DrawPixel(uint8_t x, uint8_t y, uint8_t on)
{
    if ((x >= SSD1306_WIDTH) || (y >= SSD1306_HEIGHT))
    {
        return;
    }

    uint16_t index = (uint16_t)x + ((uint16_t)(y / 8U) * SSD1306_WIDTH);

    uint8_t mask = (uint8_t)(1U << (y % 8U));

    if (on != 0U)
    {
        ssd1306_buffer[index] |= mask;
    }
    else
    {
        ssd1306_buffer[index] &= (uint8_t)(~mask);
    }
}

void SSD1306_SetCursor(uint8_t x, uint8_t y)
{
    cursorX = x;
    cursorY = y;
}

void SSD1306_WriteChar(char character)
{
    uint8_t glyph[5];

    if ((cursorX + 5U) >= SSD1306_WIDTH)
    {
        cursorX = 0U;
        cursorY = (uint8_t)(cursorY + 8U);
    }

    if ((cursorY + 7U) >= SSD1306_HEIGHT)
    {
        return;
    }

    SSD1306_GetGlyph(character, glyph);

    for (uint8_t col = 0U; col < 5U; col++)
    {
        for (uint8_t row = 0U; row < 7U; row++)
        {
            uint8_t on = (uint8_t)((glyph[col] >> row) & 0x01U);

            SSD1306_DrawPixel((uint8_t)(cursorX + col), (uint8_t)(cursorY + row), on);
        }
    }

    cursorX = (uint8_t)(cursorX + 6U);
}

void SSD1306_WriteString(const char *str)
{
    if (str == NULL)
    {
        return;
    }

    while (*str != '\0')
    {
        SSD1306_WriteChar(*str);
        str++;
    }
}

HAL_StatusTypeDef SSD1306_UpdateScreen(void)
{
    uint8_t txBuffer[SSD1306_WIDTH + 1U];

    if (ssd1306_i2c == NULL)
    {
        return HAL_ERROR;
    }

    txBuffer[0] = 0x40U; // data control byte

    for (uint8_t page = 0U; page < 8U; page++)
    {
        if (SSD1306_WriteCommand((uint8_t)(0xB0U + page)) != HAL_OK)
        {
            return HAL_ERROR;
        }

        if (SSD1306_WriteCommand(0x00U) != HAL_OK)
        {
            return HAL_ERROR;
        }

        if (SSD1306_WriteCommand(0x10U) != HAL_OK)
        {
            return HAL_ERROR;
        }

        memcpy(&txBuffer[1], &ssd1306_buffer[(uint16_t)page * SSD1306_WIDTH], SSD1306_WIDTH);

        if (HAL_I2C_Master_Transmit(ssd1306_i2c, SSD1306_I2C_ADDR, txBuffer, sizeof(txBuffer), 100U) != HAL_OK)
        {
            return HAL_ERROR;
        }
    }

    return HAL_OK;
}
