using System;

namespace VignetteCreator
{
    public class Vignetter
    {
        public static void Vignette(ref byte[] pixelArray, int index, int length, int centerX, int centerY, int radious, int gradientArea)
        {
            int rowIndex = index / length;

            for (int i = index; i < index + length; i += 4)
            {
                int cR = pixelArray[i];
                int cG = pixelArray[i + 1];
                int cB = pixelArray[i + 2];

                int columnIndex = (i - index) / 4;

                double distanceX = Math.Abs(columnIndex - centerX);
                double distanceY = Math.Abs(rowIndex - centerY);
                double distance = Math.Sqrt(Math.Pow(distanceX, 2) + Math.Pow(distanceY, 2));

                if (distance > radious && distance <= radious + gradientArea)
                {
                    int k = (int)((distance - radious) * 100 / gradientArea);
                    cR -= k;
                    cB -= k;
                    cG -= k;
                }
                else if (distance > radious + gradientArea)
                {
                    cR -= 100;
                    cB -= 100;
                    cG -= 100;
                }

                if (cR < 0) cR = 0;
                if (cG < 0) cG = 0;
                if (cB < 0) cB = 0;

                pixelArray[i] = (byte)cR;
                pixelArray[i + 1] = (byte)cG;
                pixelArray[i + 2] = (byte)cB;
            }
        }
    }
}
