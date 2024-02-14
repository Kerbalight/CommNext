#include "compute.h"
#include <math.h>

double FusedMultiplyAdd(double a, double b, double c)
{
    return fma(a, b, c);
}