//-----------------------------------------------------------------------------
// Camera Singleton that for now, doesn't do much.
//
// __Defense Sample for Game Programming Algorithms and Techniques
// Copyright (C) Sanjay Madhav. All rights reserved.
//
// Released under the Microsoft Permissive License.
// See LICENSE.txt for full details.
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace itp380
{
	public class Camera
	{
       
		Game m_Game;
		Vector3 m_vEye = new Vector3(0, 0, 100);
		Vector3 m_vTarget = Vector3.Zero;
    

        //variables needed for the spring camera
        Vector3 vDisplacement = Vector3.Zero;
        const float fSpringConstant = 128.0f;
        float fDampConstant = 2.0f *(float) Math.Sqrt(128.0f);
        Vector3 vSpringAccel = Vector3.Zero;
        Vector3 vCameraVelocity = Vector3.Zero;
        GameObject targetObject;
        Vector3 vCameraForward = Vector3.Zero;
        Vector3 vCameraUp = Vector3.Zero;
        Vector3 vCameraLeft = Vector3.Zero;
        Vector3 vIdealPosition = Vector3.Zero;

		Matrix m_Camera;
		public Matrix CameraMatrix
		{
			get { return m_Camera; }
		}

		public Camera(Game game)
		{
			m_Game = game;

			ComputeMatrix();
		}

		public void Update(float fDeltaTime)
		{
            
            // TODO: If we want a moving camera, we need to make changes here
            m_vTarget = targetObject.Position;
            vIdealPosition = m_vTarget - Vector3.Multiply(targetObject.Forward, 7 ) + Vector3.Multiply(targetObject.Up, 2 );

            //vIdealPosition = new Vector3(vIdealPosition.X, vIdealPosition.Y/* - GameState.Get().boo.idle*/, vIdealPosition.Z);

            vDisplacement = m_vEye - vIdealPosition;
            vSpringAccel = Vector3.Multiply(vDisplacement, -fSpringConstant) - Vector3.Multiply(vCameraVelocity, fDampConstant);
            vCameraVelocity += Vector3.Multiply(vSpringAccel,fDeltaTime);
            m_vEye += Vector3.Multiply(vCameraVelocity,fDeltaTime);
            ComputeMatrix();

		}

		void ComputeMatrix()
		{
            if (targetObject != null)
            {
                m_Camera = Matrix.CreateLookAt(m_vEye, m_vTarget, targetObject.Up);
            }
		}

        public void setTarget(GameObject target)
        {
            targetObject = target;
            m_vTarget = targetObject.Position;
            vIdealPosition = m_vTarget - Vector3.Multiply(targetObject.Forward, 5) + Vector3.Multiply(targetObject.Up, 5);
            m_vEye = vIdealPosition;
            ComputeMatrix();
        }
	}
}
