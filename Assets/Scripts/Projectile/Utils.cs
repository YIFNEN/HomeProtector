using UnityEngine;

public static class Utils
{
	public static float GetAngleFromPosition(Vector2 owner, Vector2 target)
	{
		// 원점으로부터의 거리와 수평축으로부터의 각도를 이용해 위치를 구하는 극 좌표계 이용
		// 각도 = arctan(y/x)
		// x, y 변위값 구하기
		float dx = target.x - owner.x;
		float dy = target.y - owner.y;

		// x, y 변위값을 바탕으로 각도 구하기
		// 각도가 radian 단위이기 때문에 Mathf.Rad2Deg를 곱해 도 단위를 구함
		float degree = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

		return degree;
	}

	/// <summary>
	/// Degree 값을 Radian 값으로 변환
	/// 1도는 "PI/180" radian
	/// angle도는 "PI/180 * angle" radian
	/// </summary>
	public static float DegreeToRadian(float angle)
	{
		return Mathf.PI * angle / 180;
	}

	public static Vector2 GetNewPoint(Vector3 start, float angle, float r)
	{
		// Degree 각도 값을 Radian으로 변경
		angle = DegreeToRadian(angle);

		// 원점을 기준으로 x, y 좌표를 구하기 때문에 시작지점 좌표(start)를 더해준다
		Vector2 position = Vector2.zero;
		position.x = Mathf.Cos(angle) * r + start.x;
		position.y = Mathf.Sin(angle) * r + start.y;

		return position;
	}

	public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
	{
		return a + (b - a) * t;
	}

	public static Vector2 QuadraticCurve(Vector2 a, Vector2 b, Vector2 c, float t)
	{
		Vector2 p1 = Lerp(a, b, t);
		Vector2 p2 = Lerp(b, c, t);

		return Lerp(p1, p2, t);
	}

	public static Vector2 CubicCurve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
	{
		Vector2 p1 = QuadraticCurve(a, b, c, t);
		Vector2 p2 = QuadraticCurve(b, c, d, t);

		return Lerp(p1, p2, t);
	}
}

