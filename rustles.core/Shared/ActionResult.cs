/*
0.  objektidentitet                     | String
1.  objektversion                       | Integer64
2.  versiongiltigfran                   | DateTime
3.  versiongiltigtill                   | DateTime
4.  objekttyp                           | String
5.  enhetstyp                           | String
6.  gransstatus                         | String
7.  lanskod                             | String
8.  kommunkod                           | String
9.  landskod                            | String
10. hogstaforvaltningsniva              | String
11. beslutatnamn                        | String
12. namnkortform                        | String
13. landareal_varde                     | Integer64
14. landareal_enhet                     | String
15. vattenareal_varde                   | Integer64
16. vattenareal_enhet                   | String
17. totalareal_varde                    | Integer64
18. totalareal_enhet                    | String
19. referensdatumfran                   | Date
20. referensdatumtill                   | Date 
 */
namespace rustles.core.Shared
{
    [Flags]
    public enum ActionResult { Ok=0, Failed=1, Miss=2, Unresolvable=4, FileMissing=8, Error = 16, UnAuthorized = 32 }
}
