﻿namespace org.apache.lucene.analysis.miscellaneous
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */



	public class TestPrefixAwareTokenFilter : BaseTokenStreamTestCase
	{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void test() throws java.io.IOException
	  public virtual void test()
	  {

		PrefixAwareTokenFilter ts;

		ts = new PrefixAwareTokenFilter(new SingleTokenTokenStream(createToken("a", 0, 1)), new SingleTokenTokenStream(createToken("b", 0, 1)));
		assertTokenStreamContents(ts, new string[] {"a", "b"}, new int[] {0, 1}, new int[] {1, 2});

		// prefix and suffix using 2x prefix

		ts = new PrefixAwareTokenFilter(new SingleTokenTokenStream(createToken("^", 0, 0)), new MockTokenizer(new StringReader("hello world"), MockTokenizer.WHITESPACE, false));
		ts = new PrefixAwareTokenFilter(ts, new SingleTokenTokenStream(createToken("$", 0, 0)));

		assertTokenStreamContents(ts, new string[] {"^", "hello", "world", "$"}, new int[] {0, 0, 6, 11}, new int[] {0, 5, 11, 11});
	  }

	  private static Token createToken(string term, int start, int offset)
	  {
		return new Token(term, start, offset);
	  }
	}

}