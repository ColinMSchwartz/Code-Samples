/**
 * Solves recursive exercises and patterns.
 * @author cschw
 *
 */
public class Recursion {
	
	/**
	 * Creates a numeric pattern.
	 * @param Number the number to be turned into a pattern.
	 * @return a string containing the generated pattern.
	 */
	public static String pattern(int number) {;
		String newString;
		if(number < 0) {
			newString = "Argument must be >= 0.";
		}
		else if(number == 1)
			newString = "1";
		else {
			newString = pattern(number - 1) + " " + number + " " + pattern(number-1);
		}
		return newString;
	}
	
	/**
	 * Creates a visual pattern of asterisks.
	 * @param number Determines how many asterisks are in the first and last rows.
	 * @return A string containing the pattern of asterisks.
	 */
	public static String hourglass(int number) {
		String newString = "";
		newString = hourglassRecurs(number, 0);
		return newString;
	}
	
	/**
	 * Separates large numbers with commas.
	 * @param number A long that is to have commas inserted into it.
	 * @return a string of the input number containing commas in the appropriate places.
	 */
	public static String commas(long number) {
		String result = "";
		result = firstDigits(number);
		return result;
	}
	
	/**
	 * Helper method for commas method.
	 * @param number the input number.
	 * @return the digits before the first comma void of zeros.
	 */
	private static String firstDigits(long number) {
		String result = "";
		if(number > -1000 && number < 1000) {
			result = Long.toString(number);
		}
		else if(number < 0){
			result = "-" + firstDigits(number / -1000) + "," + commasWithZeros(number % 1000);
		}
		else if(number > 0) {
			result = firstDigits(number / 1000) + "," + commasWithZeros(number % 1000);
		}
		return result;
	}
	
	/**
	 * Helper method for commas method.
	 * @param number the input number.
	 * @return Digits to the right of the first comma containing zeros as place-holders.
	 */
	private static String commasWithZeros(long number) {
		String result = "";
		if(number < 1000 && number > -1) {
			if(number > 9 && number < 100) {
				result = "0" + Long.toString(number);
			}
			else if(number < 10) {
				result = "00" + Long.toString(number);
			}
			else {
				result = Long.toString(number);
			}
		}
		else if(number > -1000 && number < 0) {
			result = Long.toString(-number);
		}
		else if(number < 0){
			result = "-" + commas(number / -1000) + "," + commas(number % 1000);
		}
		else if(number > 0) {
			result = commas(number / 1000) + "," + commas(number % 1000);
		}
		return result;
	}
	
	/**
	 * Helper method for the hourglass method.
	 * @param number the number of starting asterisks in the first and last rows.
	 * @param leadSpace the amount of leading spaces a row gets.
	 * @return a string containing the formatted hourglass.
	 */
	private static String builder(int number, int leadSpace) {
		String startString = "";
		if(number == 1) {
			startString = spaces(leadSpace) + "*\n";
		}
		else if(number > 1) {
			startString = spaces(leadSpace) + "* " + builder(number - 1, 0);
		}
		return startString;
	}
	
	/**
	 * Helper method that recursively builds the hourglass.
	 * @param number the number of asterisks in the first and last rows.
	 * @param leadSpace the number of leading spaces in a row.
	 * @return a string containing the formatted hourglass.
	 */
	private static String hourglassRecurs(int number, int leadSpace) {
		String newString = "";
		if(number < 1) {
			newString = "Argument must be >= 1.";
		}
		else if(number == 1) {
			newString = builder(number, leadSpace) + builder(number, leadSpace);
		}
		else {
			newString = builder(number, leadSpace) + hourglassRecurs(number - 1, leadSpace + 1) + builder(number, leadSpace);
		}
		return newString;
	}
	
	/**
	 * Helper method for the hourglass method.
	 * @param number the number of leading spaces that a line should contain.
	 * @return a string containing the amount of leading spaces.
	 */
	private static String spaces(int number) {
		String indent = "";
		if(number == 0){
			indent = "";
		}
		else {
			indent = " " + spaces(number - 1);
		}
		return indent;
	}

}
