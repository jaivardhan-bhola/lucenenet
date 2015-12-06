﻿namespace org.apache.lucene.analysis.pt
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


	using TokenStreamComponents = org.apache.lucene.analysis.Analyzer.TokenStreamComponents;
	using KeywordTokenizer = org.apache.lucene.analysis.core.KeywordTokenizer;
	using LowerCaseFilter = org.apache.lucene.analysis.core.LowerCaseFilter;
	using SetKeywordMarkerFilter = org.apache.lucene.analysis.miscellaneous.SetKeywordMarkerFilter;
	using StandardTokenizer = org.apache.lucene.analysis.standard.StandardTokenizer;
	using CharArraySet = org.apache.lucene.analysis.util.CharArraySet;

//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.apache.lucene.analysis.VocabularyAssert.*;

	/// <summary>
	/// Simple tests for <seealso cref="PortugueseMinimalStemFilter"/>
	/// </summary>
	public class TestPortugueseMinimalStemFilter : BaseTokenStreamTestCase
	{
	  private Analyzer analyzer = new AnalyzerAnonymousInnerClassHelper();

	  private class AnalyzerAnonymousInnerClassHelper : Analyzer
	  {
		  public AnalyzerAnonymousInnerClassHelper()
		  {
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer source = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
			TokenStream result = new LowerCaseFilter(TEST_VERSION_CURRENT, source);
			return new Analyzer.TokenStreamComponents(source, new PortugueseMinimalStemFilter(result));
		  }
	  }

	  /// <summary>
	  /// Test the example from the paper "Assessing the impact of stemming accuracy
	  /// on information retrieval"
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testExamples() throws java.io.IOException
	  public virtual void testExamples()
	  {
		assertAnalyzesTo(analyzer, "O debate político, pelo menos o que vem a público, parece, de modo nada " + "surpreendente, restrito a temas menores. Mas há, evidentemente, " + "grandes questões em jogo nas eleições que se aproximam.", new string[] {"o", "debate", "político", "pelo", "menos", "o", "que", "vem", "a", "público", "parece", "de", "modo", "nada", "surpreendente", "restrito", "a", "tema", "menor", "mas", "há", "evidentemente", "grande", "questão", "em", "jogo", "na", "eleição", "que", "se", "aproximam"});
	  }

	  /// <summary>
	  /// Test against a vocabulary from the reference impl </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testVocabulary() throws java.io.IOException
	  public virtual void testVocabulary()
	  {
		assertVocabulary(analyzer, getDataFile("ptminimaltestdata.zip"), "ptminimal.txt");
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testKeyword() throws java.io.IOException
	  public virtual void testKeyword()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.analysis.util.CharArraySet exclusionSet = new org.apache.lucene.analysis.util.CharArraySet(TEST_VERSION_CURRENT, asSet("quilométricas"), false);
		CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, asSet("quilométricas"), false);
		Analyzer a = new AnalyzerAnonymousInnerClassHelper2(this, exclusionSet);
		checkOneTerm(a, "quilométricas", "quilométricas");
	  }

	  private class AnalyzerAnonymousInnerClassHelper2 : Analyzer
	  {
		  private readonly TestPortugueseMinimalStemFilter outerInstance;

		  private CharArraySet exclusionSet;

		  public AnalyzerAnonymousInnerClassHelper2(TestPortugueseMinimalStemFilter outerInstance, CharArraySet exclusionSet)
		  {
			  this.outerInstance = outerInstance;
			  this.exclusionSet = exclusionSet;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer source = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
			TokenStream sink = new SetKeywordMarkerFilter(source, exclusionSet);
			return new Analyzer.TokenStreamComponents(source, new PortugueseMinimalStemFilter(sink));
		  }
	  }

	  /// <summary>
	  /// blast some random strings through the analyzer </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testRandomStrings() throws Exception
	  public virtual void testRandomStrings()
	  {
		checkRandomData(random(), analyzer, 1000 * RANDOM_MULTIPLIER);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void testEmptyTerm() throws java.io.IOException
	  public virtual void testEmptyTerm()
	  {
		Analyzer a = new AnalyzerAnonymousInnerClassHelper3(this);
		checkOneTerm(a, "", "");
	  }

	  private class AnalyzerAnonymousInnerClassHelper3 : Analyzer
	  {
		  private readonly TestPortugueseMinimalStemFilter outerInstance;

		  public AnalyzerAnonymousInnerClassHelper3(TestPortugueseMinimalStemFilter outerInstance)
		  {
			  this.outerInstance = outerInstance;
		  }

		  protected internal override Analyzer.TokenStreamComponents createComponents(string fieldName, Reader reader)
		  {
			Tokenizer tokenizer = new KeywordTokenizer(reader);
			return new Analyzer.TokenStreamComponents(tokenizer, new PortugueseMinimalStemFilter(tokenizer));
		  }
	  }
	}

}