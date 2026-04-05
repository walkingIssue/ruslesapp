using rustles.core.Entities;
using rustles.core.Entities.DTOs;
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
namespace rustles.core.Systems.GDAL.Abstractions
{
    public interface IRequestPostion<U>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="position">request position</param>
        /// <param name="progressCallback">takes responcibility for instantiating a default value of the type,
        /// will ge shoved into the callback function in the return object</param>
        /// <param name="token"></param>
        /// <returns></returns>
        PositionResolution<AssignUserRoom, U> QueResultionRequest(ClientPosition position, Action<int, U> progressCallback, CancellationToken token);
        Task<PositionResolution<AssignUserRoom, U>> QueResultionRequestAsync(ClientPosition position, Action<int, U> progressCallback, CancellationToken token);
        /// <summary>
        /// t
        /// </summary>
        /// <param name="position"></param>
        /// <exception cref="ArgumentException">original position argument used to que the task!</exception>
        void DequeRequest(ref ClientPosition clientPosition);
    }
}
