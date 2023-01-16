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
        public static void ComputeObstacle(Vector2 position_, float radius_, float timeHorizonObst_, Vector2 velocity_, List<Obstacle> obstacleNeighbors_, List<Line> orcaLines_)
        {
            float invTimeHorizonObst = 1.0f / timeHorizonObst_;
            /* Create obstacle ORCA lines. */
            for (int i=0; i<obstacleNeighbors_.Count; ++i)
            {
                Obstacle obstacle1 = obstacleNeighbors_[i];
                Obstacle obstacle2 = obstacle1.next_;
                Vector2 relativePosition1 = obstacle1.point_ - position_;
                Vector2 relativePosition2 = obstacle2.point_ - position_;

                /*
                 * Check if velocity obstacle of obstacle is already taken care
                 * of by previously constructed obstacle ORCA lines.
                 */
                //检查障碍物的速度障碍是否已经被先前建造的障碍物ORCA线所处理。
                bool alreadyCovered = false;
                for (int j = 0; j < orcaLines_.Count; ++j)
                {
                    var line = orcaLines_[j];
                    float val1 = Cross(invTimeHorizonObst * relativePosition1 - line.point, line.direction) - invTimeHorizonObst * radius_;
                    float val2 = Cross(invTimeHorizonObst * relativePosition2 - line.point, line.direction) - invTimeHorizonObst * radius_;
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
                float radiusSq = radius_ * radius_;
                Vector2 obstacleVector = obstacle2.point_ - obstacle1.point_;
                float s = Vector2.Dot(-relativePosition1, obstacleVector) / obstacleVector.sqrMagnitude;
                float distSqLine = (-relativePosition1 - s * obstacleVector).sqrMagnitude;
                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    if (obstacle1.convex_)
                    {
                        var dir = new Vector2(-relativePosition1.y, relativePosition1.x);
                        orcaLines_.Add(new Line { point = Vector2.zero, direction = dir });
                    }
                    continue;
                }
                else if (s > 1.0f && distSq2 <= radiusSq)
                {
                    /*
                     * Collision with right vertex. Ignore if non-convex or if
                     * it will be taken care of by neighboring obstacle.
                     */
                    if (obstacle2.convex_ && Cross(relativePosition2, obstacle2.direction_) >= 0.0f)
                    {
                        var dir = new Vector2(-relativePosition2.y, relativePosition2.x);
                        orcaLines_.Add(new Line { point = Vector2.zero, direction = dir });
                    }
                    continue;
                }
                else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                {
                    /* Collision with obstacle segment. */
                    orcaLines_.Add(new Line { point = Vector2.zero, direction = -obstacle1.direction_ });
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
                    if (!obstacle1.convex_)
                        continue;/* Ignore obstacle. */
                    obstacle2 = obstacle1;

                    float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, 
                        relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                    rightLegDirection = new Vector2(relativePosition1.x * leg1 + relativePosition1.y * radius_, 
                        -relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                }
                else if (s > 1.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that
                     * right vertex defines velocity obstacle.
                     */
                    if (!obstacle2.convex_)
                        continue;/* Ignore obstacle. */
                    obstacle1 = obstacle2;

                    float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                    leftLegDirection = new Vector2(relativePosition2.x * leg2 - relativePosition2.y * radius_, 
                        relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                    rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius_, 
                        -relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                }
                else
                {
                    /* Usual situation. */
                    if (obstacle1.convex_)
                    {
                        float leg1 = Mathf.Sqrt(distSq1 - radiusSq);
                        leftLegDirection = new Vector2(relativePosition1.x * leg1 - relativePosition1.y * radius_, 
                            relativePosition1.x * radius_ + relativePosition1.y * leg1) / distSq1;
                    }
                    else
                    {
                        /* Left vertex non-convex; left leg extends cut-off line. */
                        leftLegDirection = -obstacle1.direction_;
                    }

                    if (obstacle2.convex_)
                    {
                        float leg2 = Mathf.Sqrt(distSq2 - radiusSq);
                        rightLegDirection = new Vector2(relativePosition2.x * leg2 + relativePosition2.y * radius_, 
                            -relativePosition2.x * radius_ + relativePosition2.y * leg2) / distSq2;
                    }
                    else
                    {
                        /* Right vertex non-convex; right leg extends cut-off line. */
                        rightLegDirection = obstacle1.direction_;
                    }
                }
                /*
                 * Legs can never point into neighboring edge when convex
                 * vertex, take cutoff-line of neighboring edge instead. If
                 * velocity projected on "foreign" leg, no constraint is added.
                 */

                Obstacle leftNeighbor = obstacle1.previous_;

                bool isLeftLegForeign = false;
                bool isRightLegForeign = false;

                if (obstacle1.convex_ && Cross(leftLegDirection, -leftNeighbor.direction_) >= 0.0f)
                {
                    /* Left leg points into obstacle. */
                    leftLegDirection = -leftNeighbor.direction_;
                    isLeftLegForeign = true;
                }

                if (obstacle2.convex_ && Cross(rightLegDirection, obstacle2.direction_) <= 0.0f)
                {
                    /* Right leg points into obstacle. */
                    rightLegDirection = obstacle2.direction_;
                    isRightLegForeign = true;
                }

                /* Compute cut-off centers. */
                Vector2 leftCutOff = invTimeHorizonObst * (obstacle1.point_ - position_);
                Vector2 rightCutOff = invTimeHorizonObst * (obstacle2.point_ - position_);
                Vector2 cutOffVector = rightCutOff - leftCutOff;

                /* Project current velocity on velocity obstacle. */

                /* Check if current velocity is projected on cutoff circles. */
                float t = obstacle1 == obstacle2 ? 0.5f : Vector2.Dot((velocity_ - leftCutOff), cutOffVector) / cutOffVector.sqrMagnitude;
                float tLeft = Vector2.Dot((velocity_ - leftCutOff), leftLegDirection);
                float tRight = Vector2.Dot((velocity_ - rightCutOff), rightLegDirection);
                if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
                {
                    /* Project on left cut-off circle. */
                    Vector2 unitW = (velocity_ - leftCutOff).normalized;

                    orcaLines_.Add(new Line 
                    {
                        point = leftCutOff + radius_ * invTimeHorizonObst * unitW,
                        direction = new Vector2(unitW.y, -unitW.x),
                    });

                    continue;
                }
                else if (t > 1.0f && tRight < 0.0f)
                {
                    /* Project on right cut-off circle. */
                    Vector2 unitW = (velocity_ - rightCutOff).normalized;
                    orcaLines_.Add(new Line
                    {
                        point = rightCutOff + radius_ * invTimeHorizonObst * unitW,
                        direction = new Vector2(unitW.y, -unitW.x),
                    });
                    continue;
                }
                /*
                 * Project on left leg, right leg, or cut-off line, whichever is
                 * closest to velocity.
                 */
                float distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : (velocity_ - (leftCutOff + t * cutOffVector)).sqrMagnitude;
                float distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : (velocity_ - (leftCutOff + tLeft * leftLegDirection)).sqrMagnitude;
                float distSqRight = tRight < 0.0f ? float.PositiveInfinity : (velocity_ - (rightCutOff + tRight * rightLegDirection)).sqrMagnitude;
                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    /* Project on cut-off line. */
                    var line = new Line { direction = -obstacle1.direction_ };
                    line.point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-line.direction.y, line.direction.x);
                    orcaLines_.Add(line);
                    continue;
                }
                if (distSqLeft <= distSqRight)
                {
                    /* Project on left leg. */
                    if (isLeftLegForeign)
                        continue;
                    orcaLines_.Add(new Line
                    {
                        point = leftCutOff + radius_ * invTimeHorizonObst * new Vector2(-leftLegDirection.y, leftLegDirection.x),
                        direction = leftLegDirection,
                    });
                    continue;
                }
                /* Project on right leg. */
                if (isRightLegForeign)
                    continue;
                var direction = -rightLegDirection;
                orcaLines_.Add(new Line
                {
                    point = rightCutOff + radius_ * invTimeHorizonObst * new Vector2(-direction.y, direction.x),
                    direction = direction,
                });
            }
        }

        public static void ComputeAgent(Vector2 position_, float radius_, float timeHorizon_, float moveStep, Vector2 velocity_, List<Agent> agentNeighbors_, List<Line> orcaLines_)
        {
            int numObstLines = orcaLines_.Count;
            float invTimeHorizon = 1.0f / timeHorizon_;
            for (int i = 0; i < agentNeighbors_.Count; ++i)
            {
                Agent other = agentNeighbors_[i];

                Vector2 relativePosition = other.position_ - position_;
                Vector2 relativeVelocity = velocity_ - other.velocity_;
                float distSq = relativePosition.sqrMagnitude;
                float combinedRadius = radius_ + other.radius_;
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

                        line.direction = new Vector2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on legs. */
                        float leg = Mathf.Sqrt(distSq - combinedRadiusSq);

                        if (Cross(relativePosition, w) > 0.0f)
                        {
                            /* Project on left leg. */
                            line.direction = new Vector2(relativePosition.x * leg - relativePosition.y * combinedRadius, 
                                relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }
                        else
                        {
                            /* Project on right leg. */
                            line.direction = -new Vector2(relativePosition.x * leg + relativePosition.y * combinedRadius, 
                                -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }

                        float dotProduct2 = Vector2.Dot(relativeVelocity, line.direction);
                        u = dotProduct2 * line.direction - relativeVelocity;
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

                    line.direction = new Vector2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.point = velocity_ + 0.5f * u;
                orcaLines_.Add(line);
            }
        }
        
        public static Vector2 ComputeNewVelocity(List<Line> orcaLines_, int numObstLines, List<Line> projLines, float maxSpeed_, Vector2 prefVelocity_)
        {
            Vector2 newVelocity_ = Vector2.zero;
            int lineFail = linearProgram2(orcaLines_, maxSpeed_, prefVelocity_, false, ref newVelocity_);
            if (lineFail < orcaLines_.Count)
            {
                linearProgram3(orcaLines_, numObstLines, projLines, lineFail, maxSpeed_, ref newVelocity_);
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
            float dotProduct = Vector2.Dot(lines[lineNo].point, lines[lineNo].direction);
            float discriminant = (dotProduct * dotProduct) + (radius * radius) - lines[lineNo].point.sqrMagnitude;

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
                float denominator = Cross(lines[lineNo].direction, lines[i].direction);
                float numerator = Cross(lines[i].direction, lines[lineNo].point - lines[i].point);

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
                if (Vector2.Dot(optVelocity, lines[lineNo].direction) > 0.0f)
                {
                    /* Take right extreme. */
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    /* Take left extreme. */
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
            }
            else
            {
                /* Optimize closest point. */
                float t = Vector2.Dot(lines[lineNo].direction, (optVelocity - lines[lineNo].point));

                if (t < tLeft)
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    result = lines[lineNo].point + t * lines[lineNo].direction;
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
                if (Cross(lines[i].direction, lines[i].point - result) > 0.0f)
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
                if (Cross(lines[i].direction, lines[i].point - result) > distance)
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

                        float determinant = Cross(lines[i].direction, lines[j].direction);

                        if (Mathf.Abs(determinant) <= RVO_EPSILON)
                        {
                            /* Line i and line j are parallel. */
                            if (Vector2.Dot(lines[i].direction, lines[j].direction) > 0.0f)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }
                            else
                            {
                                /* Line i and line j point in opposite direction. */
                                line.point = 0.5f * (lines[i].point + lines[j].point);
                            }
                        }
                        else
                        {
                            line.point = lines[i].point + (Cross(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }

                        line.direction = (lines[j].direction - lines[i].direction).normalized;
                        projLines.Add(line);
                    }

                    Vector2 tempResult = result;
                    if (linearProgram2(projLines, radius, new Vector2(-lines[i].direction.y, lines[i].direction.x), true, ref result) < projLines.Count)
                    {
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    distance = Cross(lines[i].direction, lines[i].point - result);
                }
            }
        }
    }
}
