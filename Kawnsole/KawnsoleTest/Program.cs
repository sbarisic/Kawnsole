using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Console = Kawnsole.Kawnsole;

namespace KawnsoleTest {
	class Program {
		static void Main(string[] args) {
			Console.Initialize("data/font.png");

			Console.Write("Enter input: ");
			Console.WriteLine(Console.ReadLine());
		}
	}
}
