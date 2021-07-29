module CalculateEngine

(*Constant Definitions*)
let pi = 3.14159265358979
let e =  2.71828182845904


type Lexeme =
     PLUS
    |MINUS
    |TIMES
    |DIVIDES
    |POWER
    |LPAREN
    |RPAREN
    |NUMBER of float
    |PI
    |E
    |INVALID

(*Lexeme Number Constructor*)
let buildNum(value) = NUMBER((value))


(*  parseNum takes in a string "str" and converts every numerical or decimal character
    to a Lexeme number.  While doing so, it checks for multiple decimals.
    If "str" begins with a valid float, parseNum returns the Lexeme object with the proper 
    value and the length of the string that was parsed.
    Otherwise, returns the INVALID Lexeme.
*)
let parseNum str = 
    let rec parseNumHelp (res, decCount, err) str1 =
        if String.length str1 = 0 
        then (res, err)
        else
            match str1.[0] with
            |n when (int n) < 58 && (int n) > 47 -> parseNumHelp (res + string(n), decCount, err) (str1.[1..(String.length str1 - 1)])
            |'.' -> if decCount = 0 
                    then parseNumHelp (res + ".", 1, false) (str1.[1..(String.length str1 - 1)])
                    else parseNumHelp (res + ".", decCount + 1, true) (str1.[1..(String.length str1 - 1)])
            |_ -> (res, err)
    in
    
    let (result, hasError) = parseNumHelp ("", 0, false) str
    in
        if hasError 
        then (INVALID, String.length result) 
        else (buildNum(float(result)), String.length result)


(*  stringToLex takes in a string "str" and converts acceptable characters to their respective
    Lexeme object.  If a decimal or digit is encountered, it calls parseNum to get the Lexeme number
    of that portion of the string.
*)
let rec stringToLex str =
    let rec stringLexHelp lx str1 = 
        if String.length str1 = 0
        then lx
        else
            match str1.[0] with
            |'(' -> stringLexHelp (lx@[LPAREN]) str1.[1..((String.length str1) - 1)]
            |')' -> stringLexHelp (lx@[RPAREN]) str1.[1..((String.length str1) - 1)]
            |'+' -> stringLexHelp (lx@[PLUS]) str1.[1..(String.length str1 - 1)]
            |'-' -> stringLexHelp (lx@[MINUS]) str1.[1..(String.length str1 - 1)]
            |'*' -> stringLexHelp (lx@[TIMES]) str1.[1..(String.length str1 - 1)]
            |'/' -> stringLexHelp (lx@[DIVIDES]) str1.[1..(String.length str1 - 1)]
            |'^' -> stringLexHelp (lx@[POWER]) str1.[1..(String.length str1 - 1)]
            | n when (int n) < 58 && (int n) > 47 -> 
                let (lex, length) = parseNum str1 in
                    if lex = INVALID 
                    then [lex]
                    else stringLexHelp (lx@[lex]) str1.[length..(String.length str1 - 1)]           
            |'.' ->let (lex, length) = parseNum str1 in
                    if lex = INVALID 
                    then [lex]
                    else stringLexHelp (lx@[lex]) str1.[length..(String.length str1 - 1)]            
            |'e' -> stringLexHelp (lx@[E]) str1.[1..(String.length str1 - 1)]            
            |'p' -> if str1.[1] = 'i' 
                    then stringLexHelp (lx@[PI]) (str1.[2..(String.length str1 - 1)])
                    else [INVALID]           
            |' ' -> stringLexHelp lx (str1.[1..(String.length str1 - 1)])           
            | _ -> [INVALID]
    in stringLexHelp [] str



type ExpNode = 
    |SUM of ExpNode * ExpNode
    |DIFF of ExpNode * ExpNode
    |PROD of ExpNode * ExpNode
    |QUOT of ExpNode * ExpNode
    |POW of ExpNode * ExpNode
    |NUMBER of float
    |INPUTERR
    |OPMISMATCH
    |SYNTAX3
    |NULL
    |SYNTAX


(*  inputCheck takes in a list of Lexemes and checks if there are any INVALID lexeme
    objects, and ensures the number of LPARENs matches the number of RPARENs.
    If each number has no more than one decimal, and the number of LPARENs matches
    the number of RPARENS, it will return true.
*)
let inputCheck lx =
    let rec inputHelp parCount lx1 =
        match lx1 with
        |[] -> parCount = 0
        |hd::tl ->
            match hd with
            |INVALID -> false
            |LPAREN -> inputHelp (parCount + 1) tl
            |RPAREN -> inputHelp (parCount - 1) tl
            |_ -> inputHelp (parCount) tl

    in inputHelp 0 lx


let opCheck lx = 
    let rec opCheckHelp deltaOp lx1 =
        match lx1 with
        |[] -> deltaOp = 1
        |hd::tl ->
            match hd with
            |Lexeme.NUMBER(_) | PI | E -> opCheckHelp (deltaOp + 1) tl
            |PLUS | MINUS | TIMES | DIVIDES | POWER -> opCheckHelp (deltaOp - 1) tl
            |_ -> opCheckHelp deltaOp tl

    in  opCheckHelp 0 lx


(*  buildPrec2 takes in a list of ExpNodes "lx" and assembles expression trees involving exponentiation.
    Returns a list of ExpNodes.
*)
let buildPrec2 lx = 
    let rec buildPrec2Help lx2 buf lx1 = 
        match lx1 with
        |[] ->  if buf = NULL
                then lx2
                else lx2@[buf]
        |a::b::tl ->
            match a with
            |POW(_) -> buildPrec2Help lx2 (POW(b, buf)) (tl)
            |NUMBER(_) -> 
                if buf = NULL
                then buildPrec2Help lx2 a (b::tl)
                else buildPrec2Help (lx2@[buf]) a (b::tl)
            |(_) -> 
                if buf = NULL
                then buildPrec2Help lx2 a (b::tl)
                else buildPrec2Help (lx2@[buf]) a (b::tl)
            
        |hd::tl -> 
            if buf = NULL
            then buildPrec2Help lx2 hd (tl)
            else buildPrec2Help (lx2@[buf]) hd (tl)
        

    in List.rev (buildPrec2Help [] NULL (List.rev lx))


(*  buildPrec1 takes in a list of ExpNodes "lx" and assembles expression trees involving products
    or quotients.  Returns a list of ExpNodes.
*)
let buildPrec1 lx = 
    let rec buildPrec1Help lx2 buf lx1 =
        match lx1 with
        |[] ->  if buf = NULL
                then lx2
                else lx2@[buf]
        |a::b::tl ->
            match a with
            |PROD(_) -> buildPrec1Help lx2 (PROD(buf, b)) tl
            |QUOT(_) -> buildPrec1Help lx2 (QUOT(buf, b)) tl
            |NUMBER(_) ->   
                if buf = NULL
                then buildPrec1Help lx2 a (b::tl)
                else buildPrec1Help (lx2@[buf]) a (b::tl)
            |_ -> 
                if buf = NULL
                then buildPrec1Help lx2 a (b::tl)
                else buildPrec1Help (lx2@[buf]) a (b::tl)
        |hd::tl -> 
                if buf = NULL
                then buildPrec1Help lx2 hd (tl)
                else buildPrec1Help (lx2@[buf]) hd (tl)

    in buildPrec1Help [] NULL lx


(*  buildPrec0 takes in a list of ExpNodes "lx" and assembles an expression tree involving sums
    and differences.  If the number of operaters matches the number of terms, the returned list of
    expression nodes should only contain one ExpNode.
*)
let buildPrec0 lx = 
    let rec buildPrec0Help lx2 buf lx1 =
        match lx1 with
        |[] ->  if buf = NULL
                then lx2
                else lx2@[buf]
        |a::b::tl ->
            match a with
            |SUM(_) -> buildPrec0Help lx2 (SUM(buf, b)) tl
            |DIFF(_) -> buildPrec0Help lx2 (DIFF(buf, b)) tl
            |NUMBER(_) -> 
                if buf = NULL
                then buildPrec0Help lx2 a (b::tl)
                else buildPrec0Help (lx2@[buf]) a (b::tl)
            |_ -> 
                if buf = NULL
                then buildPrec0Help lx2 a (b::tl)
                else buildPrec0Help (lx2@[buf]) a (b::tl)
        |hd::tl -> buildPrec0Help (lx2@[hd]) NULL (tl)

    in buildPrec0Help [] NULL lx  


(*  singleTreeCheck takes in a list of ExpNodes "ls" and checks if there is only one element.
    This is done after every tree of each operator precidence is called in order.  If "ls" contains
    only one element, that element is returned.  Otherwise, returns an INPUTERR ExpNode for no elements,
    or returns OPMISMATCH if there is more than one element.
*)
let singleTreeCheck ls =
    match ls with
    |[] -> INPUTERR
    |[hd] -> hd
    |hd::_ -> OPMISMATCH 


(*  nodesToTree takes in an ExpNode list and returns the final expression tree.  If there are errors,
    returns what singleTreeCheck returns.
*)
let nodesToTree lx = 
    lx |> buildPrec2 |> buildPrec1 |> buildPrec0 |> singleTreeCheck
    


(*  lexToNodes takes in a list of Lexemes "lx" and converts them to their respective expression nodes in
    order to build an expression tree.
    Is mutually recursive with parToExp.
*)
let rec lexToNodes lx= 
    let rec lexNodeHelp exp lx1 = 
        match lx1 with
        |[] ->  exp
        |hd::tl -> 
            match hd with
            |Lexeme.NUMBER value -> lexNodeHelp (exp@[(NUMBER(value))]) tl
            |PI -> lexNodeHelp (exp@[(NUMBER(pi))]) tl
            |E -> lexNodeHelp (exp@[(NUMBER(e))]) tl
            |PLUS -> lexNodeHelp (exp@[SUM(NUMBER(0.),NUMBER(0.))]) tl
            |MINUS -> lexNodeHelp (exp@[DIFF(NUMBER(0.),NUMBER(0.))]) tl
            |TIMES -> lexNodeHelp (exp@[PROD(NUMBER(0.),NUMBER(0.))]) tl
            |DIVIDES -> lexNodeHelp (exp@[QUOT(NUMBER(0.),NUMBER(0.))]) tl
            |POWER -> lexNodeHelp (exp@[POW(NUMBER(0.),NUMBER(0.))]) tl
            |LPAREN -> 
                let (pars, rem) = parToExp tl
                in lexNodeHelp (exp@[pars]) rem
            |_ -> [INPUTERR]

    in lexNodeHelp [] lx


(*  parToExp takes in a list of lexemes and returns a tuple of (ExpNode, Lexeme List).  The 
    ExpNode is what is inside of the parens in the form of a formatted expression tree.  The
    Lexeme list is the remaining lexemes to the right of the first RPAREN.
    Is mutually recursive with lexToNodes.
*)
and parToExp lx2 =
    let rec parExpHelp exp lx3 =
        match lx3 with 
        |[] -> ([INVALID],[])
        |hd::tl ->  
          if hd = RPAREN
          then (exp, tl)
          else (parExpHelp (exp@[hd]) tl)
    in 
    
    let (parens, remainder) = parExpHelp [] lx2
    in
        ((parens |> lexToNodes |> nodesToTree), remainder)


(*  evaluate evaluates an expression tree and returns the resulting float.
*)
let rec evaluate tree = 
    match tree with
    |POW (l,r) -> (evaluate l) ** (evaluate r)
    |PROD (l,r) -> (evaluate l) * (evaluate r)
    |QUOT (l,r) -> (evaluate l) / (evaluate r)
    |SUM (l,r) -> (evaluate l) + (evaluate r)
    |DIFF (l,r) -> (evaluate l) - (evaluate r)
    |NUMBER (v) -> v
    |_ -> 0.0
 

(*  masterCalculate takes in a string, converts string to lexemes, checks for invalid characters, duplicate decimals,
    and valid syntax.  Returns corresponding error message if any check has failed.  Otherwise, lexemes are converted
    to ExpNodes, ExpNodes are converted into an expression tree, tree is evaluated, then finally returns a string
    representation of the resulting float.
*)
let masterCalculate str = 
    let lex = stringToLex str
    in
        if not (inputCheck lex) then "Invalid Input"
        else
            
            if not (opCheck lex) then "Operator Mismatch"
            else
           
                lex |> lexToNodes |> nodesToTree |> evaluate |> string

