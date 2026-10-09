from error_message_compactor import ErrorCompactionOptions, compact_error_entries, high_frequency_candidates


def test_high_frequency_errors_keep_project_frame_and_emit_review_candidate() -> None:
    entries = [
        {"level": "Exception", "message": "NullReferenceException: probe", "stack": ["UnityEngine.Debug.Log", "Probe.Run() (at Assets/Probe.cs:42)"]}
        for _ in range(4)
    ]
    compact = compact_error_entries(entries, ErrorCompactionOptions(max_entries=5, max_stack_frames=5))
    assert compact[0].count == 4
    assert compact[0].source == "Assets/Probe.cs:42"
    assert "Assets/Probe.cs:42" in compact[0].stack[0]
    candidate = high_frequency_candidates(compact)[0]
    assert candidate["requiresReview"] is True
    assert candidate["count"] == 4
