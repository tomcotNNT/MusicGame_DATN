using System;

public static class TutorialEvents
{
    public static Action NormalNoteHit;      // nốt thường được đánh trúng
    public static Action LongNoteStarted;    // bắt đầu giữ nốt dài
    public static Action LongNoteCompleted;  // giữ xong nốt dài
}