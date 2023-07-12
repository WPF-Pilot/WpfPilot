#pragma warning disable

namespace WpfPilot.Tests;

using NUnit.Framework;

[TestFixture]
public sealed class PrimitiveTests
{
	[Test]
	public void TestEquality()
	{
		Primitive number1 = 1;
		Primitive number2 = 1.0;
		Primitive number3 = 1.0f;
		Primitive string1 = "Hello world";
		Primitive string2 = "Hello world";

		// Number equality tests.
		Assert.True(number1 == number2);
		Assert.True(number1 == number3);
		Assert.True(number1 == 1);

		Assert.AreEqual(number1, 1);
		Assert.AreEqual(1, number1);

		Assert.AreNotEqual(number1, number2); // 1.Equals(1.0) == false
		Assert.AreNotEqual(number1, number3);

		Assert.True(number1 == number3);
		Assert.AreNotEqual(number1, number3);

		// String equality tests.
		Assert.True(string1 == string2);
		Assert.AreEqual(string1, string2);
		Assert.AreEqual("Hello world", string1);
		Assert.AreEqual(string1, "Hello world");
	}

	[Test]
	public void TestNumberAndBoolOperations()
	{
		Primitive number1 = 1;
		Primitive number2 = 2;

		// Test `+`.
		Assert.True(number1 + number2 == 3);
		Assert.True(number1 + 2 == 3);
		Assert.AreEqual(3, number1 + number2);
		Assert.AreEqual(3, number1 + 2);

		// Test `>`
		Assert.True(number2 > number1);
		Assert.True(number2 > 1);
		Assert.True(100 > number2);

		// Test `>=`
		Assert.True(number2 >= number1);
		Assert.True(number2 >= 1);
		Assert.True(100 >= number2);

		// Test `<`
		Assert.True(number1 < number2);
		Assert.True(number1 < 2);
		Assert.True(1 < number2);

		// Test `<=`
		Assert.True(number1 <= number2);
		Assert.True(number1 <= 2);
		Assert.True(1 <= number2);

		// Test `-`
		Assert.True(number2 - number1 == 1);
		Assert.True(number2 - 1 == 1);

		// Test `*`
		Assert.True(number2 * number1 == 2);
		Assert.True(number2 * 1 == 2);

		// Test `/`
		Assert.True(number2 / number1 == 2);
		Assert.True(number2 / 1 == 2);

		// Test `%`
		Assert.True(number2 % number1 == 0);
		Assert.True(number2 % 1 == 0);

		// Test `^`
		Assert.True((number2 ^ number1) == 3);
		Assert.True((number2 ^ 1) == 3);

		// Test `&`
		Assert.True((number2 & number1) == 0);
		Assert.True((number2 & 1) == 0);

		// Test `|`
		Assert.True((number2 | number1) == 3);
		Assert.True((number2 | 1) == 3);

		// Test `~`
		Assert.True(~number1 == -2);

		// Test `<<`
		Assert.True((number2 << number1) == 4);
		Assert.True((number2 << 1) == 4);

		// Test `>>`
		Assert.True((number2 >> number1) == 1);
		Assert.True((number2 >> 1) == 1);

		// Test `!=`
		Assert.True(number2 != number1);
		Assert.True(number2 != 1);

		// Test `!`
		Primitive boolTrue = true;
		Primitive boolFalse = false;
		Assert.True(boolTrue);
		Assert.False(!boolTrue);
		Assert.False(boolFalse);
		Assert.True(!boolFalse);

		// Test `+=`
		Primitive numberPlusEqual = 1;
		numberPlusEqual += 1;
		Assert.AreEqual(2, numberPlusEqual);

		// Test `-=`
		Primitive numberMinusEqual = 2;
		numberMinusEqual -= 1;
		Assert.AreEqual(1, numberMinusEqual);

		// Test `*=`
		Primitive numberMultiplyEqual = 1;
		numberMultiplyEqual *= 2;
		Assert.AreEqual(2, numberMultiplyEqual);

		// Test `/=`
		Primitive numberDivisionEqual = 2;
		numberDivisionEqual /= 2;
		Assert.AreEqual(1, numberDivisionEqual);

		// Test `%=`
		Primitive numberModuloEqual = 2;
		numberModuloEqual %= 1;
		Assert.AreEqual(0, numberModuloEqual);

		// Test `^=`
		Primitive numberXorEqual = 1;
		numberXorEqual ^= 1;
		Assert.AreEqual(0, numberXorEqual);

		// Test `&=`
		Primitive numberAndEqual = 1;
		numberAndEqual &= 1;
		Assert.AreEqual(1, numberAndEqual);

		// Test `|=`
		Primitive numberOrEqual = 1;
		numberOrEqual |= 1;
		Assert.AreEqual(1, numberOrEqual);

		// Test `<<=`
		Primitive numberLeftShiftEqual = 1;
		numberLeftShiftEqual <<= 1;
		Assert.AreEqual(2, numberLeftShiftEqual);

		// Test `>>=`
		Primitive numberRightShiftEqual = 2;
		numberRightShiftEqual >>= 1;
		Assert.AreEqual(1, numberRightShiftEqual);

		// Test `++`
		Primitive numberIncrement = 0;
		Assert.AreEqual(1, ++numberIncrement);

		// Test `--`
		Primitive numberDecrement = 2;
		Assert.AreEqual(1, --numberDecrement);
	}
}
