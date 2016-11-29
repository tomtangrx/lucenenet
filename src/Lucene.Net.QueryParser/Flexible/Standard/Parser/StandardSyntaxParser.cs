﻿using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
{
    /// <summary>
    /// Parser for the standard Lucene syntax
    /// </summary>
    public class StandardSyntaxParser : ISyntaxParser /*, StandardSyntaxParserConstants*/
    {
        private static readonly int CONJ_NONE = 0;
        private static readonly int CONJ_AND = 2;
        private static readonly int CONJ_OR = 2;


        // syntax parser constructor
        public StandardSyntaxParser()
            : this(new FastCharStream(new StringReader("")))
        {
        }
        /** Parses a query string, returning a {@link org.apache.lucene.queryparser.flexible.core.nodes.QueryNode}.
        *  @param query  the query string to be parsed.
        *  @throws ParseException if the parsing fails
        */
        public IQueryNode Parse(string query, string field)
        {
            ReInit(new FastCharStream(new StringReader(query)));
            try
            {
                // TopLevelQuery is a Query followed by the end-of-input (EOF)
                IQueryNode querynode = TopLevelQuery(field);
                return querynode;
            }
            catch (ParseException tme)
            {
                tme.SetQuery(query);
                throw tme;
            }
            catch (Exception tme)
            {
                IMessage message = new MessageImpl(QueryParserMessages.INVALID_SYNTAX_CANNOT_PARSE, query, tme.Message);
                QueryNodeParseException e = new QueryNodeParseException(tme);
                e.SetQuery(query);
                e.SetNonLocalizedMessage(message);
                throw e;
            }
        }

        // *   Query  ::= ( Clause )*
        // *   Clause ::= ["+", "-"] [<TERM> ":"] ( <TERM> | "(" Query ")" )
        public int Conjunction()
        {
            int ret = CONJ_NONE;
            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
            {
                case RegexpToken.AND:
                case RegexpToken.OR:
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.AND:
                            jj_consume_token(RegexpToken.AND);
                            ret = CONJ_AND;
                            break;
                        case RegexpToken.OR:
                            jj_consume_token(RegexpToken.OR);
                            ret = CONJ_OR;
                            break;
                        default:
                            jj_la1[0] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    break;
                default:
                    jj_la1[1] = jj_gen;
                    break;
            }
            { if (true) return ret; }
            throw new Exception("Missing return statement in function");
        }

        public Modifier Modifiers()
        {
            Modifier ret = Modifier.MOD_NONE;
            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
            {
                case RegexpToken.NOT:
                case RegexpToken.PLUS:
                case RegexpToken.MINUS:
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.PLUS:
                            jj_consume_token(RegexpToken.PLUS);
                            ret = Modifier.MOD_REQ;
                            break;
                        case RegexpToken.MINUS:
                            jj_consume_token(RegexpToken.MINUS);
                            ret = Modifier.MOD_NOT;
                            break;
                        case RegexpToken.NOT:
                            jj_consume_token(RegexpToken.NOT);
                            ret = Modifier.MOD_NOT;
                            break;
                        default:
                            jj_la1[2] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    break;
                default:
                    jj_la1[3] = jj_gen;
                    break;
            }
            { if (true) return ret; }
            throw new Exception("Missing return statement in function");
        }

        // This makes sure that there is no garbage after the query string
        public IQueryNode TopLevelQuery(string field)
        {
            IQueryNode q;
            q = Query(field);
            jj_consume_token(0);
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        // These changes were made to introduce operator precedence:
        // - Clause() now returns a QueryNode. 
        // - The modifiers are consumed by Clause() and returned as part of the QueryNode Object
        // - Query does not consume conjunctions (AND, OR) anymore. 
        // - This is now done by two new non-terminals: ConjClause and DisjClause
        // The parse tree looks similar to this:
        //       Query ::= DisjQuery ( DisjQuery )*
        //   DisjQuery ::= ConjQuery ( OR ConjQuery )* 
        //   ConjQuery ::= Clause ( AND Clause )*
        //      Clause ::= [ Modifier ] ... 
        public IQueryNode Query(string field)
        {
            List<IQueryNode> clauses = null;
            IQueryNode c, first = null;
            first = DisjQuery(field);
            
            while (true)
            {
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.NOT:
                    case RegexpToken.PLUS:
                    case RegexpToken.MINUS:
                    case RegexpToken.LPAREN:
                    case RegexpToken.QUOTED:
                    case RegexpToken.TERM:
                    case RegexpToken.REGEXPTERM:
                    case RegexpToken.RANGEIN_START:
                    case RegexpToken.RANGEEX_START:
                    case RegexpToken.NUMBER:
                        ;
                        break;
                    default:
                        jj_la1[4] = jj_gen;
                        goto label_1_break;
                }
                c = DisjQuery(field);
                if (clauses == null)
                {
                    clauses = new List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            label_1_break:
            if (clauses != null)
            {
                { if (true) return new BooleanQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode DisjQuery(string field)
        {
            IQueryNode first, c;
            List<IQueryNode> clauses = null;
            first = ConjQuery(field);
            
            while (true)
            {
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.OR:
                        ;
                        break;
                    default:
                        jj_la1[5] = jj_gen;
                        goto label_2_break;
                }
                jj_consume_token(RegexpToken.OR);
                c = ConjQuery(field);
                if (clauses == null)
                {
                    clauses = new List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            label_2_break:
            if (clauses != null)
            {
                { if (true) return new OrQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode ConjQuery(string field)
        {
            IQueryNode first, c;
            List<IQueryNode> clauses = null;
            first = ModClause(field);
            
            while (true)
            {
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.AND:
                        ;
                        break;
                    default:
                        jj_la1[6] = jj_gen;
                        goto label_3_break;
                }
                jj_consume_token(RegexpToken.AND);
                c = ModClause(field);
                if (clauses == null)
                {
                    clauses = new List<IQueryNode>();
                    clauses.Add(first);
                }
                clauses.Add(c);
            }
            label_3_break:
            if (clauses != null)
            {
                { if (true) return new AndQueryNode(clauses); }
            }
            else
            {
                { if (true) return first; }
            }
            throw new Exception("Missing return statement in function");
        }

        // QueryNode Query(CharSequence field) :
        // {
        // List clauses = new ArrayList();
        //   List modifiers = new ArrayList();
        //   QueryNode q, firstQuery=null;
        //   ModifierQueryNode.Modifier mods;
        //   int conj;
        // }
        // {
        //   mods=Modifiers() q=Clause(field)
        //   {
        //     if (mods == ModifierQueryNode.Modifier.MOD_NONE) firstQuery=q;
        //     
        //     // do not create modifier nodes with MOD_NONE
        //      if (mods != ModifierQueryNode.Modifier.MOD_NONE) {
        //        q = new ModifierQueryNode(q, mods);
        //      }
        //      clauses.add(q);
        //   }
        //   (
        //     conj=Conjunction() mods=Modifiers() q=Clause(field)
        //     { 
        //       // do not create modifier nodes with MOD_NONE
        //        if (mods != ModifierQueryNode.Modifier.MOD_NONE) {
        //          q = new ModifierQueryNode(q, mods);
        //        }
        //        clauses.add(q);
        //        //TODO: figure out what to do with AND and ORs
        //   }
        //   )*
        //     {
        //      if (clauses.size() == 1 && firstQuery != null)
        //         return firstQuery;
        //       else {
        //       return new BooleanQueryNode(clauses);
        //       }
        //     }
        // }
        public IQueryNode ModClause(string field)
        {
            IQueryNode q;
            Modifier mods;
            mods = Modifiers();
            q = Clause(field);
            if (mods != Modifier.MOD_NONE)
            {
                q = new ModifierQueryNode(q, mods);
            }
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode Clause(string field)
        {
            IQueryNode q;
            Token fieldToken = null, boost = null, @operator = null, term = null;
            FieldQueryNode qLower, qUpper;
            bool lowerInclusive, upperInclusive;

            bool group = false;
            if (jj_2_2(3))
            {
                fieldToken = jj_consume_token(RegexpToken.TERM);
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.OP_COLON:
                    case RegexpToken.OP_EQUAL:
                        switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                        {
                            case RegexpToken.OP_COLON:
                                jj_consume_token(RegexpToken.OP_COLON);
                                break;
                            case RegexpToken.OP_EQUAL:
                                jj_consume_token(RegexpToken.OP_EQUAL);
                                break;
                            default:
                                jj_la1[7] = jj_gen;
                                jj_consume_token(-1);
                                throw new ParseException();
                        }
                        field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image).ToString();
                        q = Term(field);
                        break;
                    case RegexpToken.OP_LESSTHAN:
                    case RegexpToken.OP_LESSTHANEQ:
                    case RegexpToken.OP_MORETHAN:
                    case RegexpToken.OP_MORETHANEQ:
                        switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                        {
                            case RegexpToken.OP_LESSTHAN:
                                @operator = jj_consume_token(RegexpToken.OP_LESSTHAN);
                                break;
                            case RegexpToken.OP_LESSTHANEQ:
                                @operator = jj_consume_token(RegexpToken.OP_LESSTHANEQ);
                                break;
                            case RegexpToken.OP_MORETHAN:
                                @operator = jj_consume_token(RegexpToken.OP_MORETHAN);
                                break;
                            case RegexpToken.OP_MORETHANEQ:
                                @operator = jj_consume_token(RegexpToken.OP_MORETHANEQ);
                                break;
                            default:
                                jj_la1[8] = jj_gen;
                                jj_consume_token(-1);
                                throw new ParseException();
                        }
                        field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image).ToString();
                        switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                        {
                            case RegexpToken.TERM:
                                term = jj_consume_token(RegexpToken.TERM);
                                break;
                            case RegexpToken.QUOTED:
                                term = jj_consume_token(RegexpToken.QUOTED);
                                break;
                            case RegexpToken.NUMBER:
                                term = jj_consume_token(RegexpToken.NUMBER);
                                break;
                            default:
                                jj_la1[9] = jj_gen;
                                jj_consume_token(-1);
                                throw new ParseException();
                        }
                        if (term.kind == RegexpToken.QUOTED)
                        {
                            term.image = term.image.Substring(1, (term.image.Length - 1) - 1);
                        }
                        switch (@operator.kind)
                        {
                            case RegexpToken.OP_LESSTHAN:
                                lowerInclusive = true;
                                upperInclusive = false;

                                qLower = new FieldQueryNode(field,
                                                           "*", term.beginColumn, term.endColumn);
                                qUpper = new FieldQueryNode(field,
                                                     EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);

                                break;
                            case RegexpToken.OP_LESSTHANEQ:
                                lowerInclusive = true;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field,
                                                         "*", term.beginColumn, term.endColumn);
                                qUpper = new FieldQueryNode(field,
                                                         EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);
                                break;
                            case RegexpToken.OP_MORETHAN:
                                lowerInclusive = false;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field,
                                                         EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);
                                qUpper = new FieldQueryNode(field,
                                                         "*", term.beginColumn, term.endColumn);
                                break;
                            case RegexpToken.OP_MORETHANEQ:
                                lowerInclusive = true;
                                upperInclusive = true;

                                qLower = new FieldQueryNode(field,
                                                         EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);
                                qUpper = new FieldQueryNode(field,
                                                         "*", term.beginColumn, term.endColumn);
                                break;
                            default:
                                { if (true) throw new Exception("Unhandled case: operator=" + @operator.ToString()); }
                        }
                        q = new TermRangeQueryNode(qLower, qUpper, lowerInclusive, upperInclusive);
                        break;
                    default:
                        jj_la1[10] = jj_gen;
                        jj_consume_token(-1);
                        throw new ParseException();
                }
            }
            else
            {
                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                {
                    case RegexpToken.LPAREN:
                    case RegexpToken.QUOTED:
                    case RegexpToken.TERM:
                    case RegexpToken.REGEXPTERM:
                    case RegexpToken.RANGEIN_START:
                    case RegexpToken.RANGEEX_START:
                    case RegexpToken.NUMBER:
                        if (jj_2_1(2))
                        {
                            fieldToken = jj_consume_token(RegexpToken.TERM);
                            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                            {
                                case RegexpToken.OP_COLON:
                                    jj_consume_token(RegexpToken.OP_COLON);
                                    break;
                                case RegexpToken.OP_EQUAL:
                                    jj_consume_token(RegexpToken.OP_EQUAL);
                                    break;
                                default:
                                    jj_la1[11] = jj_gen;
                                    jj_consume_token(-1);
                                    throw new ParseException();
                            }
                            field = EscapeQuerySyntaxImpl.DiscardEscapeChar(fieldToken.image).ToString();
                        }
                        else
                        {
                            ;
                        }
                        switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                        {
                            case RegexpToken.QUOTED:
                            case RegexpToken.TERM:
                            case RegexpToken.REGEXPTERM:
                            case RegexpToken.RANGEIN_START:
                            case RegexpToken.RANGEEX_START:
                            case RegexpToken.NUMBER:
                                q = Term(field);
                                break;
                            case RegexpToken.LPAREN:
                                jj_consume_token(RegexpToken.LPAREN);
                                q = Query(field);
                                jj_consume_token(RegexpToken.RPAREN);
                                switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                                {
                                    case RegexpToken.CARAT:
                                        jj_consume_token(RegexpToken.CARAT);
                                        boost = jj_consume_token(RegexpToken.NUMBER);
                                        break;
                                    default:
                                        jj_la1[12] = jj_gen;
                                        break;
                                }
                                group = true;
                                break;
                            default:
                                jj_la1[13] = jj_gen;
                                jj_consume_token(-1);
                                throw new ParseException();
                        }
                        break;
                    default:
                        jj_la1[14] = jj_gen;
                        jj_consume_token(-1);
                        throw new ParseException();
                }
            }
            if (boost != null)
            {
                float f = (float)1.0;
                try
                {
                    f = Convert.ToSingle(boost.image, CultureInfo.InvariantCulture);
                    // avoid boosting null queries, such as those caused by stop words
                    if (q != null)
                    {
                        q = new BoostQueryNode(q, f);
                    }
                }
                catch (Exception ignored)
                {
                    /* Should this be handled somehow? (defaults to "no boost", if
                         * boost number is invalid)
                         */
                }
            }
            if (group) { q = new GroupQueryNode(q); }
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        public IQueryNode Term(string field)
        {
            Token term, boost = null, fuzzySlop = null, goop1, goop2;
            bool fuzzy = false;
            bool regexp = false;
            bool startInc = false;
            bool endInc = false;
            IQueryNode q = null;
            FieldQueryNode qLower, qUpper;
            float defaultMinSimilarity = Search.FuzzyQuery.DefaultMinSimilarity;
            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
            {
                case RegexpToken.TERM:
                case RegexpToken.REGEXPTERM:
                case RegexpToken.NUMBER:
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.TERM:
                            term = jj_consume_token(RegexpToken.TERM);
                            q = new FieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), term.beginColumn, term.endColumn);
                            break;
                        case RegexpToken.REGEXPTERM:
                            term = jj_consume_token(RegexpToken.REGEXPTERM);
                            regexp = true;
                            break;
                        case RegexpToken.NUMBER:
                            term = jj_consume_token(RegexpToken.NUMBER);
                            break;
                        default:
                            jj_la1[15] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.FUZZY_SLOP:
                            fuzzySlop = jj_consume_token(RegexpToken.FUZZY_SLOP);
                            fuzzy = true;
                            break;
                        default:
                            jj_la1[16] = jj_gen;
                            break;
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.CARAT:
                            jj_consume_token(RegexpToken.CARAT);
                            boost = jj_consume_token(RegexpToken.NUMBER);
                            switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                            {
                                case RegexpToken.FUZZY_SLOP:
                                    fuzzySlop = jj_consume_token(RegexpToken.FUZZY_SLOP);
                                    fuzzy = true;
                                    break;
                                default:
                                    jj_la1[17] = jj_gen;
                                    break;
                            }
                            break;
                        default:
                            jj_la1[18] = jj_gen;
                            break;
                    }
                    if (fuzzy)
                    {
                        float fms = defaultMinSimilarity;
                        try
                        {
                            fms = Convert.ToSingle(fuzzySlop.image.Substring(1), CultureInfo.InvariantCulture);
                        }
                        catch (Exception ignored) { }
                        if (fms < 0.0f)
                        {
                            { if (true) throw new ParseException(new MessageImpl(QueryParserMessages.INVALID_SYNTAX_FUZZY_LIMITS)); }
                        }
                        else if (fms >= 1.0f && fms != (int)fms)
                        {
                            { if (true) throw new ParseException(new MessageImpl(QueryParserMessages.INVALID_SYNTAX_FUZZY_EDITS)); }
                        }
                        q = new FuzzyQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image), fms, term.beginColumn, term.endColumn);
                    }
                    else if (regexp)
                    {
                        string re = term.image.Substring(1, (term.image.Length - 1) - 1);
                        q = new RegexpQueryNode(field, re, 0, re.Length);
                    }
                    break;
                case RegexpToken.RANGEIN_START:
                case RegexpToken.RANGEEX_START:
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGEIN_START:
                            jj_consume_token(RegexpToken.RANGEIN_START);
                            startInc = true;
                            break;
                        case RegexpToken.RANGEEX_START:
                            jj_consume_token(RegexpToken.RANGEEX_START);
                            break;
                        default:
                            jj_la1[19] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGE_GOOP:
                            goop1 = jj_consume_token(RegexpToken.RANGE_GOOP);
                            break;
                        case RegexpToken.RANGE_QUOTED:
                            goop1 = jj_consume_token(RegexpToken.RANGE_QUOTED);
                            break;
                        default:
                            jj_la1[20] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGE_TO:
                            jj_consume_token(RegexpToken.RANGE_TO);
                            break;
                        default:
                            jj_la1[21] = jj_gen;
                            break;
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGE_GOOP:
                            goop2 = jj_consume_token(RegexpToken.RANGE_GOOP);
                            break;
                        case RegexpToken.RANGE_QUOTED:
                            goop2 = jj_consume_token(RegexpToken.RANGE_QUOTED);
                            break;
                        default:
                            jj_la1[22] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.RANGEIN_END:
                            jj_consume_token(RegexpToken.RANGEIN_END);
                            endInc = true;
                            break;
                        case RegexpToken.RANGEEX_END:
                            jj_consume_token(RegexpToken.RANGEEX_END);
                            break;
                        default:
                            jj_la1[23] = jj_gen;
                            jj_consume_token(-1);
                            throw new ParseException();
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.CARAT:
                            jj_consume_token(RegexpToken.CARAT);
                            boost = jj_consume_token(RegexpToken.NUMBER);
                            break;
                        default:
                            jj_la1[24] = jj_gen;
                            break;
                    }
                    if (goop1.kind == RegexpToken.RANGE_QUOTED)
                    {
                        goop1.image = goop1.image.Substring(1, (goop1.image.Length - 1) - 1);
                    }
                    if (goop2.kind == RegexpToken.RANGE_QUOTED)
                    {
                        goop2.image = goop2.image.Substring(1, (goop2.image.Length - 1) - 1);
                    }

                    qLower = new FieldQueryNode(field,
                                             EscapeQuerySyntaxImpl.DiscardEscapeChar(goop1.image), goop1.beginColumn, goop1.endColumn);
                    qUpper = new FieldQueryNode(field,
                                                 EscapeQuerySyntaxImpl.DiscardEscapeChar(goop2.image), goop2.beginColumn, goop2.endColumn);
                    q = new TermRangeQueryNode(qLower, qUpper, startInc ? true : false, endInc ? true : false);
                    break;
                case RegexpToken.QUOTED:
                    term = jj_consume_token(RegexpToken.QUOTED);
                    q = new QuotedFieldQueryNode(field, EscapeQuerySyntaxImpl.DiscardEscapeChar(term.image.Substring(1, (term.image.Length - 1) - 1)), term.beginColumn + 1, term.endColumn - 1);
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.FUZZY_SLOP:
                            fuzzySlop = jj_consume_token(RegexpToken.FUZZY_SLOP);
                            break;
                        default:
                            jj_la1[25] = jj_gen;
                            break;
                    }
                    switch ((jj_ntk == -1) ? Jj_ntk() : jj_ntk)
                    {
                        case RegexpToken.CARAT:
                            jj_consume_token(RegexpToken.CARAT);
                            boost = jj_consume_token(RegexpToken.NUMBER);
                            break;
                        default:
                            jj_la1[26] = jj_gen;
                            break;
                    }
                    int phraseSlop = 0;

                    if (fuzzySlop != null)
                    {
                        try
                        {
                            phraseSlop = (int)Convert.ToSingle(fuzzySlop.image.Substring(1), CultureInfo.InvariantCulture);
                            q = new SlopQueryNode(q, phraseSlop);
                        }
                        catch (Exception ignored)
                        {
                            /* Should this be handled somehow? (defaults to "no PhraseSlop", if
                           * slop number is invalid)
                           */
                        }
                    }
                    break;
                default:
                    jj_la1[27] = jj_gen;
                    jj_consume_token(-1);
                    throw new ParseException();
            }
            if (boost != null)
            {
                float f = (float)1.0;
                try
                {
                    f = Convert.ToSingle(boost.image, CultureInfo.InvariantCulture);
                    // avoid boosting null queries, such as those caused by stop words
                    if (q != null)
                    {
                        q = new BoostQueryNode(q, f);
                    }
                }
                catch (Exception ignored)
                {
                    /* Should this be handled somehow? (defaults to "no boost", if
                       * boost number is invalid)
                       */
                }
            }
            { if (true) return q; }
            throw new Exception("Missing return statement in function");
        }

        private bool jj_2_1(int xla)
        {
            jj_la = xla; jj_lastpos = jj_scanpos = token;
            try { return !jj_3_1(); }
            catch (LookaheadSuccess ls) { return true; }
            finally { jj_save(0, xla); }
        }

        private bool jj_2_2(int xla)
        {
            jj_la = xla; jj_lastpos = jj_scanpos = token;
            try { return !jj_3_2(); }
            catch (LookaheadSuccess ls) { return true; }
            finally { jj_save(1, xla); }
        }

        private bool jj_3_2()
        {
            if (jj_scan_token(RegexpToken.TERM)) return true;
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_4())
            {
                jj_scanpos = xsp;
                if (jj_3R_5()) return true;
            }
            return false;
        }

        private bool jj_3R_12()
        {
            if (jj_scan_token(RegexpToken.RANGEIN_START)) return true;
            return false;
        }

        private bool jj_3R_11()
        {
            if (jj_scan_token(RegexpToken.REGEXPTERM)) return true;
            return false;
        }

        private bool jj_3_1()
        {
            if (jj_scan_token(RegexpToken.TERM)) return true;
            Token xsp;
            xsp = jj_scanpos;
            if (jj_scan_token(15))
            {
                jj_scanpos = xsp;
                if (jj_scan_token(16)) return true;
            }
            return false;
        }

        private bool jj_3R_8()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_12())
            {
                jj_scanpos = xsp;
                if (jj_scan_token(27)) return true;
            }
            return false;
        }

        private bool jj_3R_10()
        {
            if (jj_scan_token(RegexpToken.TERM)) return true;
            return false;
        }

        private bool jj_3R_7()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_10())
            {
                jj_scanpos = xsp;
                if (jj_3R_11())
                {
                    jj_scanpos = xsp;
                    if (jj_scan_token(28)) return true;
                }
            }
            return false;
        }

        private bool jj_3R_9()
        {
            if (jj_scan_token(RegexpToken.QUOTED)) return true;
            return false;
        }

        private bool jj_3R_5()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_scan_token(17))
            {
                jj_scanpos = xsp;
                if (jj_scan_token(18))
                {
                    jj_scanpos = xsp;
                    if (jj_scan_token(19))
                    {
                        jj_scanpos = xsp;
                        if (jj_scan_token(20)) return true;
                    }
                }
            }
            xsp = jj_scanpos;
            if (jj_scan_token(23))
            {
                jj_scanpos = xsp;
                if (jj_scan_token(22))
                {
                    jj_scanpos = xsp;
                    if (jj_scan_token(28)) return true;
                }
            }
            return false;
        }

        private bool jj_3R_4()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_scan_token(15))
            {
                jj_scanpos = xsp;
                if (jj_scan_token(16)) return true;
            }
            if (jj_3R_6()) return true;
            return false;
        }

        private bool jj_3R_6()
        {
            Token xsp;
            xsp = jj_scanpos;
            if (jj_3R_7())
            {
                jj_scanpos = xsp;
                if (jj_3R_8())
                {
                    jj_scanpos = xsp;
                    if (jj_3R_9()) return true;
                }
            }
            return false;
        }

        /** Generated Token Manager. */
        public StandardSyntaxParserTokenManager token_source;
        /** Current token. */
        public Token token;
        /** Next token. */
        public Token jj_nt;
        private int jj_ntk;
        private Token jj_scanpos, jj_lastpos;
        private int jj_la;
        private int jj_gen;
        readonly private int[] jj_la1 = new int[28];
        static private uint[] jj_la1_0;
        static private int[] jj_la1_1;
        static StandardSyntaxParser()
        {
            jj_la1_init_0();
            jj_la1_init_1();
        }
        private static void jj_la1_init_0()
        {
            jj_la1_0 = new uint[] { 0x300, 0x300, 0x1c00, 0x1c00, 0x1ec03c00, 0x200, 0x100, 0x18000, 0x1e0000, 0x10c00000, 0x1f8000, 0x18000, 0x200000, 0x1ec02000, 0x1ec02000, 0x12800000, 0x1000000, 0x1000000, 0x200000, 0xc000000, 0x0, 0x20000000, 0x0, 0xc0000000, 0x200000, 0x1000000, 0x200000, 0x1ec00000, };
        }
        private static void jj_la1_init_1()
        {
            jj_la1_1 = new int[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x3, 0x0, 0x3, 0x0, 0x0, 0x0, 0x0, 0x0, };
        }
        readonly private JJCalls[] jj_2_rtns = new JJCalls[2];
        private bool jj_rescan = false;
        private int jj_gc = 0;

        /** Constructor with user supplied CharStream. */
        public StandardSyntaxParser(ICharStream stream)
        {
            token_source = new StandardSyntaxParserTokenManager(stream);
            token = new Token();
            jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Reinitialise. */
        public void ReInit(ICharStream stream)
        {
            token_source.ReInit(stream);
            token = new Token();
            jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Constructor with generated Token Manager. */
        public StandardSyntaxParser(StandardSyntaxParserTokenManager tm)
        {
            token_source = tm;
            token = new Token();
            jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        /** Reinitialise. */
        public void ReInit(StandardSyntaxParserTokenManager tm)
        {
            token_source = tm;
            token = new Token();
            jj_ntk = -1;
            jj_gen = 0;
            for (int i = 0; i < 28; i++) jj_la1[i] = -1;
            for (int i = 0; i < jj_2_rtns.Length; i++) jj_2_rtns[i] = new JJCalls();
        }

        private Token jj_consume_token(int kind)
        {
            Token oldToken;
            if ((oldToken = token).next != null) token = token.next;
            else token = token.next = token_source.getNextToken();
            jj_ntk = -1;
            if (token.kind == kind)
            {
                jj_gen++;
                if (++jj_gc > 100)
                {
                    jj_gc = 0;
                    for (int i = 0; i < jj_2_rtns.Length; i++)
                    {
                        JJCalls c = jj_2_rtns[i];
                        while (c != null)
                        {
                            if (c.gen < jj_gen) c.first = null;
                            c = c.next;
                        }
                    }
                }
                return token;
            }
            token = oldToken;
            jj_kind = kind;
            throw generateParseException();
        }

        internal sealed class LookaheadSuccess : Exception { }
        readonly private LookaheadSuccess jj_ls = new LookaheadSuccess();
        private bool jj_scan_token(int kind)
        {
            if (jj_scanpos == jj_lastpos)
            {
                jj_la--;
                if (jj_scanpos.next == null)
                {
                    jj_lastpos = jj_scanpos = jj_scanpos.next = token_source.getNextToken();
                }
                else
                {
                    jj_lastpos = jj_scanpos = jj_scanpos.next;
                }
            }
            else
            {
                jj_scanpos = jj_scanpos.next;
            }
            if (jj_rescan)
            {
                int i = 0; Token tok = token;
                while (tok != null && tok != jj_scanpos) { i++; tok = tok.next; }
                if (tok != null) jj_add_error_token(kind, i);
            }
            if (jj_scanpos.kind != kind) return true;
            if (jj_la == 0 && jj_scanpos == jj_lastpos) throw jj_ls;
            return false;
        }


        /** Get the next Token. */
        public Token getNextToken()
        {
            if (token.next != null) token = token.next;
            else token = token.next = token_source.getNextToken();
            jj_ntk = -1;
            jj_gen++;
            return token;
        }

        /** Get the specific Token. */
        public Token getToken(int index)
        {
            Token t = token;
            for (int i = 0; i < index; i++)
            {
                if (t.next != null) t = t.next;
                else t = t.next = token_source.getNextToken();
            }
            return t;
        }

        private int Jj_ntk()
        {
            if ((jj_nt = token.next) == null)
                return (jj_ntk = (token.next = token_source.getNextToken()).kind);
            else
                return (jj_ntk = jj_nt.kind);
        }

        private List<int[]> jj_expentries = new List<int[]>();
        private int[] jj_expentry;
        private int jj_kind = -1;
        private int[] jj_lasttokens = new int[100];
        private int jj_endpos;

        private void jj_add_error_token(int kind, int pos)
        {
            if (pos >= 100) return;
            if (pos == jj_endpos + 1)
            {
                jj_lasttokens[jj_endpos++] = kind;
            }
            else if (jj_endpos != 0)
            {
                jj_expentry = new int[jj_endpos];
                for (int i = 0; i < jj_endpos; i++)
                {
                    jj_expentry[i] = jj_lasttokens[i];
                }
                 for (var it = jj_expentries.GetEnumerator(); it.MoveNext();)
                {
                    int[] oldentry = (int[])(it.Current);
                    if (oldentry.Length == jj_expentry.Length)
                    {
                        for (int i = 0; i < jj_expentry.Length; i++)
                        {
                            if (oldentry[i] != jj_expentry[i])
                            {
                                goto jj_entries_loop_continue;
                            }
                        }
                        jj_expentries.Add(jj_expentry);
                        goto jj_entries_loop_break;
                    }
                    jj_entries_loop_continue: { }
                }
                jj_entries_loop_break:
                if (pos != 0) jj_lasttokens[(jj_endpos = pos) - 1] = kind;
            }
        }

        /** Generate ParseException. */
        public virtual ParseException generateParseException()
        {
            jj_expentries.Clear();
            bool[] la1tokens = new bool[34];
            if (jj_kind >= 0)
            {
                la1tokens[jj_kind] = true;
                jj_kind = -1;
            }
            for (int i = 0; i < 28; i++)
            {
                if (jj_la1[i] == jj_gen)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        if ((jj_la1_0[i] & (1 << j)) != 0)
                        {
                            la1tokens[j] = true;
                        }
                        if ((jj_la1_1[i] & (1 << j)) != 0)
                        {
                            la1tokens[32 + j] = true;
                        }
                    }
                }
            }
            for (int i = 0; i < 34; i++)
            {
                if (la1tokens[i])
                {
                    jj_expentry = new int[1];
                    jj_expentry[0] = i;
                    jj_expentries.Add(jj_expentry);
                }
            }
            jj_endpos = 0;
            jj_rescan_token();
            jj_add_error_token(0, 0);
            int[][] exptokseq = new int[jj_expentries.Count][];
            for (int i = 0; i < jj_expentries.Count; i++)
            {
                exptokseq[i] = jj_expentries[i];
            }
            return new ParseException(token, exptokseq, StandardSyntaxParserConstants.TokenImage);
        }

        /** Enable tracing. */
        public void enable_tracing()
        {
        }

        /** Disable tracing. */
        public void disable_tracing()
        {
        }

        private void jj_rescan_token()
        {
            jj_rescan = true;
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    JJCalls p = jj_2_rtns[i];
                    do
                    {
                        if (p.gen > jj_gen)
                        {
                            jj_la = p.arg; jj_lastpos = jj_scanpos = p.first;
                            switch (i)
                            {
                                case 0: jj_3_1(); break;
                                case 1: jj_3_2(); break;
                            }
                        }
                        p = p.next;
                    } while (p != null);
                }
                catch (LookaheadSuccess ls) { }
            }
            jj_rescan = false;
        }

        private void jj_save(int index, int xla)
        {
            JJCalls p = jj_2_rtns[index];
            while (p.gen > jj_gen)
            {
                if (p.next == null) { p = p.next = new JJCalls(); break; }
                p = p.next;
            }
            p.gen = jj_gen + xla - jj_la; p.first = token; p.arg = xla;
        }

        internal sealed class JJCalls
        {
            internal int gen;
            internal Token first;
            internal int arg;
            internal JJCalls next;
        }
    }
}