//Clean
using WolfensteinInfinite.Engine.Graphics;

namespace WolfensteinInfinite.Utilities
{
    public static class Matrices
    {
        public static float[,] CreateIdentityMatrix(int length)
        {
            float[,] matrix = new float[length, length];

            for (int i = 0, j = 0; i < length; i++, j++)
                matrix[i, j] = 1;

            return matrix;
        }
        public static float[,] Multiply(float[,] matrix1, float[,] matrix2)
        {
            // cahing matrix lengths for better performance  
            var matrix1Rows = matrix1.GetLength(0);
            var matrix1Cols = matrix1.GetLength(1);
            var matrix2Rows = matrix2.GetLength(0);
            var matrix2Cols = matrix2.GetLength(1);

            // checking if product is defined  
            if (matrix1Cols != matrix2Rows)
                throw new InvalidOperationException
                  ("Product is undefined. n columns of first matrix must equal to n rows of second matrix");

            // creating the final product matrix  
            float[,] product = new float[matrix1Rows, matrix2Cols];

            // looping through matrix 1 rows  
            for (int matrix1_row = 0; matrix1_row < matrix1Rows; matrix1_row++)
            {
                // for each matrix 1 row, loop through matrix 2 columns  
                for (int matrix2_col = 0; matrix2_col < matrix2Cols; matrix2_col++)
                {
                    // loop through matrix 1 columns to calculate the dot product  
                    for (int matrix1_col = 0; matrix1_col < matrix1Cols; matrix1_col++)
                    {
                        product[matrix1_row, matrix2_col] +=
                          matrix1[matrix1_row, matrix1_col] *
                          matrix2[matrix1_col, matrix2_col];
                    }
                }
            }

            return product;
        }
        public static float[,] MultiplyUnsafe(float[,] matrix1, float[,] matrix2)
        {
            // cahing matrix lengths for better performance  
            var matrix1Rows = matrix1.GetLength(0);
            var matrix1Cols = matrix1.GetLength(1);
            var matrix2Rows = matrix2.GetLength(0);
            var matrix2Cols = matrix2.GetLength(1);

            // checking if product is defined  
            if (matrix1Cols != matrix2Rows)
                throw new InvalidOperationException
                  ("Product is undefined. n columns of first matrix must equal to n rows of second matrix");

            // creating the final product matrix  
            float[,] product = new float[matrix1Rows, matrix2Cols];

            unsafe
            {
                // fixing pointers to matrices  
                fixed (
                  float* pProduct = product,
                  pMatrix1 = matrix1,
                  pMatrix2 = matrix2)
                {

                    int i = 0;
                    // looping through matrix 1 rows  
                    for (int matrix1_row = 0; matrix1_row < matrix1Rows; matrix1_row++)
                    {
                        // for each matrix 1 row, loop through matrix 2 columns  
                        for (int matrix2_col = 0; matrix2_col < matrix2Cols; matrix2_col++)
                        {
                            // loop through matrix 1 columns to calculate the dot product  
                            for (int matrix1_col = 0; matrix1_col < matrix1Cols; matrix1_col++)
                            {

                                var val1 = *(pMatrix1 + matrix1Rows * matrix1_row + matrix1_col);
                                var val2 = *(pMatrix2 + matrix2Cols * matrix1_col + matrix2_col);

                                *(pProduct + i) += val1 * val2;

                            }

                            i++;

                        }
                    }

                }
            }

            return product;
        }

        /// <summary>  
        /// Combines transformations to create single transformation matrix  
        /// </summary>  
        public static float[,] CreateTransformationMatrix
          (IImageTransformation[] vectorTransformations, int dimensions)
        {
            float[,] vectorTransMatrix =
              CreateIdentityMatrix(dimensions);

            // combining transformations works by multiplying them  
            foreach (var trans in vectorTransformations)
                vectorTransMatrix =
                  MultiplyUnsafe(vectorTransMatrix, trans.CreateTransformationMatrix());

            return vectorTransMatrix;
        }
        
    }
}
