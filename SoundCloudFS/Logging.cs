
using System;

namespace btEngine
{


	public class Logging
	{

		public Logging ()
		{
		}
		
		public static void Write(string what)
		{
			if(btEngine.Engine.Config.DisplayDebug)
			{
				System.Console.WriteLine("{0}", what);
			}
		}
	}
}
