using System.Collections.Generic;
using UnityEngine;

namespace SDFNav.ORCA
{
    public static class ORCAUtil
    {
        const float RVO_EPSILON = 0.00001f;
        private static float Cross(Vector2 vector1, Vector2 vector2)
        {
            return vector1.x * vector2.y - vector1.y * vector2.x;
        }
        public static void ComputeObstacle(Vector2 position, float radius, float timeHorizonObst, Vector2 velocity, List<Obstacle> obstacleNeighbors, List<Line> orcaLines)
        {
            float invTimeHorizonObst = 1.0f / timeHorizonObst;
            /* Create obstacle ORCA lines. */
            for (int i=0; i<obstacleNeighbors.Count; ++i)
            {
                Obstacle obstacle1 = obstacleNeighbors[i];
                Obstacle obstacle2 = obstacle1.Next;
                Vector2 relativePosition1 = obstacle1.Point - position;
                Vector2 relativePosition2 = obstacle2.Point - position;

                /*
                 * Check if velocity obstacle of obstacle is already taken care
                 * of by previously constructed obstacle ORCA lines.
                 */
                //检查障碍物的速度障碍是否已经被先前建造的障碍物ORCA线所处理。
                bool alreadyCovered = false;
                for (int j = 0; j < orcaLines.Count; ++j)
                {
                    var line = orcaLines[j];
                    float val1 = Cross(invTimeHorizonObst * relativePosition1 - line.Point, line.Direction) - invTimeHorizonObst * radius;
                    float val2 = Cross(invTimeHorizonObst * relativePosition2 - line.Point, line.Direction) - invTimeHorizonObst * radius;
                    if (val1 >=-RVO_EPSILON && val2 >= -RVO_EPSILON)
                    {
                        alreadyCovered = true;
                        break;
                    }
                }
                if (alreadyCovered)
                    continue;

                /* Not yet covered. Check for collisions. */
                float distSq1 = relativePosition1.sqrMagnitude;
                float distSq2 = relativePosition2.sqrMagnitude;
                float radiusSq = radius * radius;
                Vector2 obstacleVector = obstacle2.Point - obstacle1.Point;
                float s = Vector2.Dot(-relativePosition1, obstacleVector) / obstacleVector.sqrMagnitude;
                float distSqLine = (-relativePosition1 - s * obstacleVector).sqrMagnitude;
                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    if (obstacle1.Convex)
                    {
                        var dir = new Vector2(-relativePosition1.y, relativePosition1.x);
                        orcaLines.Add(new Line { Point = Vector2.zero, Direction = dir });
                    }
                    continue;
                }
                else if (s > 1.0f && distSq2 <= radiusSq)
                {
                    /*
                     * Collision with right vertex. Ignore if non-convex or if
                     * it will be taken care of by neighboring obstacle.
                     */
                    if (obstacle2.Convex && Cross(relativePosition2, obstacle2.Direction) >= 0.0f)
                    {
                        var dir = new Vector2(-relativePosition2.y, relativePosition2.x);
                        orcaLines.Add(new Line { Point = Vector2.zero, Direction = dir });
                    }
                    continue;
                }
                else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                {
                    /* Collision with obstacle segment. */
                    orcaLines.Add(new Line { Point = Vector2.zero, Direction = -obstacle1.Direction });
                    continue;
                }
                /*
                 * No collision. Compute legs. When obliquely viewed, both legs
                 * can come from a single vertex. Legs extend cut-off line when
                 * non-convex vertex.
                 */
                Vector2 leftLegDirection, rightLegDirection;
                if (s < 0.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that left vertex
                     * defines velocity obstacle.
                     */
                    if (!obstacle1.Convex)
                        continue;/* Ignore obstacle. */
                    obstacle2 = obstacle1;

                    float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius, 
                        relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                    rightLegDirection = new Vector2(relativePosition1.x * leg1 + relativePosition1.y * radius, 
                        -relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                }
                else if (s > 1.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that
                     * right vertex defines velocity obstacle.
                     */
                    if (!obstacle2.Convex)
                        continue;/* Ignore obstacle. */
                    obstacle1 = obstacle2;

                    float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition2.x * leg2 - relativePosition2.y * radius, 
                        relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                    rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius, 
                        -relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                }
                else
                {
                    /* Usual situation. */
                    if (obstacle1.Convex)
                    {
                        float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                        leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius, 
                            relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                    }
                    else
                    {
                        /* Left vertex non-convex; left leg extends cut-off line. */
                        leftLegDirection = -obstacle1.Direction;
                    }

                    if (obstacle2.Convex)
                    {
                        float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                        rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius, 
                            -relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                    }
                    else
                    {
                        /* Right vertex non-convex; right leg extends cut-off line. */
                        rightLegDirection = obstacle1.Direction;
                    }
                }
                /*
                 * Legs can never point into neighboring edge when convex
                 * vertex, take cutoff-line of neighboring edge instead. If
                 * velocity projected on "foreign" leg, no constraint is added.
                 */

                Obstacle leftNeighbor = obstacle1.Previous;

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                if (obstacle1.Convex && Cross(leftLegDirection, -leftNeighbor.Direction) >= 0.0f)
                {
                    /* Left leg points into obstacle. */
                    leftLegDirection = -leftNeighbor.Direction;
                    isLeftLegForeign = true;
                }

                if (obstacle2.Convex && Cross(rightLegDirection, obstacle2.Direction) <= 0.0f)
                {
                    /* Right leg points into obstacle. */
                    rightLegDirection = obstacle2.Direction;
                    isRightLegForeign = true;
                }

                /* Compute cut-off centers. */
                Vector2 leftCutOff = invTimeHorizonObst * (obstacle1.Point - position);
                Vector2 rightCutOff = invTimeHorizonObst * (obstacle2.Point - position);
                Vector2 cutOffVector = rightCutOff - leftCutOff;

                /* Project current velocity on velocity obstacle. */

                /* Check if current velocity is projected on cutoff circles. */
                float t = obstacle1 == obstacle2 ? 0.5f : Vector2.Dot((velocity - leftCutOff), cutOffVector) / cutOffVector.sqrMagnitude;
                float tLeft = Vector2.Dot((velocity - leftCutOff), leftLegDirection);
                float tRight = Vector2.Dot((velocity - rightCutOff), rightLegDirection);
                if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
                {
                    /* Project on left cut-off circle. */
                    Vector2 unitW = (velocity - leftCutOff).normalized;

                    orcaLines.Add(new Line 
                    {
                        Point = leftCutOff + radius * invTimeHorizonObst * unitW,
                        Direction = new Vector2(unitW.y, -unitW.x),
                    });

                    continue;
                }
                else if (t > 1.0f && tRight < 0.0f)
                {
                    /* Project on right cut-off circle. */
                    Vector2 unitW = (velocity - rightCutOff).normalized;
                    orcaLines.Add(new Line
                    {
                        Point = rightCutOff + radius * invTimeHorizonObst * unitW,
                        Direction = new Vector2(unitW.y, -unitW.x),
                    });
                    continue;
                }
                /*
                 * Project on left leg, right leg, or cut-off line, whichever is
                 * closest to velocity.
                 */
                float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : (velocity - (leftCutOff + t * cutOffVector)).sqrMagnitude;
                float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : (velocity - (leftCutOff + tLeft * leftLegDirection)).sqrMagnitude;
                float distSqRight = tRight < 0.0f ? float.PositiveInfinity : (velocity - (rightCutOff + tRight * rightLegDirection)).sqrMagnitude;
                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    /* Project on cut-off line. */
                    var line = new Line { Direction = -obstacle1.Direction };
                    line.Point = leftCutOff + radius * invTimeHorizonObst * new Vector2(-line.Direction.y, line.Direction.x);
                    orcaLines.Add(line);
                    continue;
                }
                if (distSqLeft <= distSqRight)
                {
                    /* Project on left leg. */
                    if (isLeftLegForeign)
                        continue;
                    orcaLines.Add(new Line
                    {
                        Point = leftCutOff + radius * invTimeHorizonObst * new Vector2(-leftLegDirection.y, leftLegDirection.x),
                        Direction = leftLegDirection,
                    });
                    continue;
                }
                /* Project on right leg. */
                if (isRightLegForeign)
                    continue;
                var direction = -rightLegDirection;
                orcaLines.Add(new Line
                {
                    Point = rightCutOff + radius * invTimeHorizonObst * new Vector2(-direction.y, direction.x),
                    Direction = direction,
                });
            }
        }

        public static void ComputeAgent(Vector2 position, float radius, float timeHorizon, float moveStep, Vector2 velocity, List<Agent> agentNeighbors, List<Line> orcaLines)
        {
            int numObstLines = orcaLines.Count;
            float invTimeHorizon = 1.0f / timeHorizon;
            for (int i = 0; i < agentNeighbors.Count; ++i)
            {
                Agent other = agentNeighbors[i];

                Vector2 relativePosition = other.Position - position;
                Vector2 relativeVelocity = velocity - other.Velocity;
                float distSq = relativePosition.sqrMagnitude;
                float combinedRadius = radius + other.Radius;
                float combinedRadiusSq = combinedRadius * combinedRadius;
                Line line;
                Vector2 u;
                if (distSq > combinedRadiusSq)
                {
                    /* No collision. */
                    Vector2 w = relativeVelocity - invTimeHorizon * relativePosition;
                    /* Vector from cutoff center to relative velocity. */
                    float wLengthSq = w.sqrMagnitude;
                    float dotProduct1 = Vector2.Dot(w, relativePosition);
                    if (dotProduct1 < 0.0f && dotProduct1* dotProduct1 > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        float wLength = UnityEngine.Mathf.Sqrt(wLengthSq);
                        Vector2 unitW = w / wLength;

                        line.Direction = new Vector2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on legs. */
                        float leg = Mathf.Sqrt(distSq - combinedRadiusSq);

                        if (Cross(relativePosition, w) > 0.0f)
                        {
                            /* Project on left leg. */
                            line.Direction = new Vector2(relativePosition.x * leg - relativePosition.y * combinedRadius, 
                                relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }
                        else
                        {
                            /* Project on right leg. */
                            line.Direction = -new Vector2(relativePosition.x * leg + relativePosition.y * combinedRadius, 
                                -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }

                        float dotProduct2 = Vector2.Dot(relativeVelocity, line.Direction);
                        u = dotProduct2 * line.Direction - relativeVelocity;
                    }
                }
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */
                    float invTimeStep = 1.0f / moveStep;

                    /* Vector from cutoff center to relative velocity. */
                    Vector2 w = relativeVelocity - invTimeStep * relativePosition;

                    float wLength = w.magnitude;
                    Vector2 unitW = w / wLength;

                    line.Direction = new Vector2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.Point = velocity + 0.5f * u;
                orcaLines.Add(line);
            }
        }
        
        public static Vector2 ComputeNewVelocity(List<Line> orcaLines, int numObstLines, List<Line> projLines, float maxSpeed, Vector2 prefVelocity)
        {
            Vector2 newVelocity_ = Vector2.zero;
            int lineFail = linearProgram2(orcaLines, maxSpeed, prefVelocity, false, ref newVelocity_);
            if (lineFail < orcaLines.Count)
            {
                linearProgram3(orcaLines, numObstLines, projLines, lineFail, maxSpeed, ref newVelocity_);
            }
            return newVelocity_;
        }

        /**
         * <summary>Solves a one-dimensional linear program on a specified line
         * subject to linear constraints defined by lines and a circular
         * constraint.</summary>
         *
         * <returns>True if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="lineNo">The specified line constraint.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private static bool linearProgram1(IList<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            float dotProduct = Vector2.Dot(lines[lineNo].Point, lines[lineNo].Direction);
            float discriminant = (dotProduct * dotProduct) + (radius * radius) - lines[lineNo].Point.sqrMagnitude;

            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                float denominator = Cross(lines[lineNo].Direction, lines[i].Direction);
                float numerator = Cross(lines[i].Direction, lines[lineNo].Point - lines[i].Point);

                if (Mathf.Abs(denominator) <= RVO_EPSILON)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    if (numerator < 0.0f)
                        return false;
                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    /* Line i bounds line lineNo on the right. */
                    tRight = Mathf.Min(tRight, t);
                }
                else
                {
                    /* Line i bounds line lineNo on the left. */
                    tLeft = Mathf.Max(tLeft, t);
                }

                if (tLeft > tRight)
                    return false;
            }

            if (directionOpt)
            {
                /* Optimize direction. */
                if (Vector2.Dot(optVelocity, lines[lineNo].Direction) > 0.0f)
                {
                    /* Take right extreme. */
                    result = lines[lineNo].Point + tRight * lines[lineNo].Direction;
                }
                else
                {
                    /* Take left extreme. */
                    result = lines[lineNo].Point + tLeft * lines[lineNo].Direction;
                }
            }
            else
            {
                /* Optimize closest point. */
                float t = Vector2.Dot(lines[lineNo].Direction, (optVelocity - lines[lineNo].Point));

                if (t < tLeft)
                {
                    result = lines[lineNo].Point + tLeft * lines[lineNo].Direction;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].Point + tRight * lines[lineNo].Direction;
                }
                else
                {
                    result = lines[lineNo].Point + t * lines[lineNo].Direction;
                }
            }

            return true;
        }
        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <returns>The number of the line it fails on, and the number of lines
         * if successful.</returns>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="optVelocity">The optimization velocity.</param>
         * <param name="directionOpt">True if the direction should be optimized.
         * </param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private static int linearProgram2(IList<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            if (directionOpt)
            {
                /*
                 * Optimize direction. Note that the optimization velocity is of
                 * unit length in this case.
                 */
                result = optVelocity * radius;
            }
            else if (optVelocity.sqrMagnitude > (radius * radius))
            {
                /* Optimize closest point and outside circle. */
                result = optVelocity.normalized * radius;
            }
            else
            {
                /* Optimize closest point and inside circle. */
                result = optVelocity;
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                if (Cross(lines[i].Direction, lines[i].Point - result) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    Vector2 tempResult = result;
                    if (!linearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;

                        return i;
                    }
                }
            }

            return lines.Count;
        }

        /**
         * <summary>Solves a two-dimensional linear program subject to linear
         * constraints defined by lines and a circular constraint.</summary>
         *
         * <param name="lines">Lines defining the linear constraints.</param>
         * <param name="projLines">GC优化的空列表</param>
         * <param name="numObstLines">Count of obstacle lines.</param>
         * <param name="beginLine">The line on which the 2-d linear program
         * failed.</param>
         * <param name="radius">The radius of the circular constraint.</param>
         * <param name="result">A reference to the result of the linear program.
         * </param>
         */
        private static void linearProgram3(IList<Line> lines, int numObstLines, IList<Line> projLines, int beginLine, float radius, ref Vector2 result)
        {
            float distance = 0.0f;

            for (int i = beginLine; i < lines.Count; ++i)
            {
                if (Cross(lines[i].Direction, lines[i].Point - result) > distance)
                {
                    projLines?.Clear();
                    projLines ??= new List<Line>();
                    /* Result does not satisfy constraint of line i. */
                    for (int ii = 0; ii < numObstLines; ++ii)
                    {
                        projLines.Add(lines[ii]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {
                        Line line;

                        float determinant = Cross(lines[i].Direction, lines[j].Direction);

                        if (Mathf.Abs(determinant) <= RVO_EPSILON)
                        {
                            /* Line i and line j are parallel. */
                            if (Vector2.Dot(lines[i].Direction, lines[j].Direction) > 0.0f)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }
                            else
                            {
                                /* Line i and line j point in opposite direction. */
                                line.Point = 0.5f * (lines[i].Point + lines[j].Point);
                            }
                        }
                        else
                        {
                            line.Point = lines[i].Point + (Cross(lines[j].Direction, lines[i].Point - lines[j].Point) / determinant) * lines[i].Direction;
                        }

                        line.Direction = (lines[j].Direction - lines[i].Direction).normalized;
                        projLines.Add(line);
                    }

                    Vector2 tempResult = result;
                    if (linearProgram2(projLines, radius, new Vector2(-lines[i].Direction.y, lines[i].Direction.x), true, ref result) < projLines.Count)
                    {
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    distance = Cross(lines[i].Direction, lines[i].Point - result);
                }
            }
        }
    }
}
