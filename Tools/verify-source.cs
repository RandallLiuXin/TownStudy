try{BuilderSetup.Verify();}catch(System.Exception e){System.IO.File.WriteAllText("Reports/source-verification-failure.txt",e.ToString());} return "Verified; see report";
